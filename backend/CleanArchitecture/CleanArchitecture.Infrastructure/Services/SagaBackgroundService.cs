using CleanArchitecture.Core.DTOs.Events;
using CleanArchitecture.Core.DTOs.Saga;
using CleanArchitecture.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Services
{
    /// <summary>
    /// SAGA komutlarını RabbitMQ kuyruğundan tüketen arka plan servisi.
    /// 
    /// Bu servis, asenkron orchestrator'ın kalbidir:
    /// - SagaController HTTP isteğini alır → RabbitMQ kuyruğuna komut yayınlar → 202 Accepted döner
    /// - Bu BackgroundService komutu kuyruktan alır → SAGA adımını yürütür → sonraki adımı tetikler
    /// 
    /// Kuyruk: gateway.saga.commands
    /// Exchange: gateway.events (topic)
    /// Routing keys:
    ///   - saga.command.start           → Checkout + Payment init başlat
    ///   - saga.command.payment-callback → Payment callback işle
    ///   - saga.command.confirm          → Restoran onayı + capture
    ///   - saga.command.reject           → Restoran reddi + void
    ///   - saga.command.cancel           → Müşteri iptali + kompanasyon
    /// </summary>
    public class SagaBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly RabbitMqConnectionService _connectionService;
        private readonly ILogger<SagaBackgroundService> _logger;

        public const string SagaCommandQueue = "gateway.saga.commands";

        // Dead Letter Queue — başarısız mesajlar burada birikir (manuel inceleme için)
        public const string DlxExchangeName  = "gateway.saga.dlx";
        public const string DlqQueueName     = "gateway.saga.deadletter";

        // Komut tipleri
        public const string CMD_START             = "saga.command.start";
        public const string CMD_PAYMENT_CALLBACK  = "saga.command.payment-callback";
        public const string CMD_CONFIRM           = "saga.command.confirm";
        public const string CMD_REJECT            = "saga.command.reject";
        public const string CMD_CANCEL            = "saga.command.cancel";

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public SagaBackgroundService(
            IServiceScopeFactory scopeFactory,
            RabbitMqConnectionService connectionService,
            ILogger<SagaBackgroundService> logger)
        {
            _scopeFactory      = scopeFactory;
            _connectionService = connectionService;
            _logger            = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[SagaWorker] Asenkron SAGA worker başlatılıyor...");

            // RabbitMQ bağlantısı hazır olana kadar bekle
            IChannel channel = null;
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    channel = await _connectionService.GetChannelAsync();

                    // ── Dead Letter Exchange kurulumu ──────────────────────────────────
                    // Başarısız komutlar DLQ'ya düşer — kaybolmaz, manuel incelenebilir.
                    // NOT: gateway.saga.commands kuyrukta daha önce DLX argümanı olmadan
                    //      oluşturulduysa RabbitMQ Management UI'dan silinmesi gerekir.
                    await channel.ExchangeDeclareAsync(
                        exchange: DlxExchangeName,
                        type: ExchangeType.Direct,
                        durable: true,
                        autoDelete: false);

                    await channel.QueueDeclareAsync(
                        queue: DlqQueueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false);

                    await channel.QueueBindAsync(
                        queue: DlqQueueName,
                        exchange: DlxExchangeName,
                        routingKey: SagaCommandQueue);

                    // Komut kuyruğunu DLX argumanıyla oluştur
                    await channel.QueueDeclareAsync(
                        queue: SagaCommandQueue,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: new System.Collections.Generic.Dictionary<string, object>
                        {
                            ["x-dead-letter-exchange"]    = DlxExchangeName,
                            ["x-dead-letter-routing-key"] = SagaCommandQueue
                        });

                    // Tüm saga.command.* routing key'lerini dinle
                    await channel.QueueBindAsync(
                        queue: SagaCommandQueue,
                        exchange: RabbitMqConnectionService.ExchangeName,
                        routingKey: "saga.command.*");

                    // MassTransit Event Bindings (Fanout Exchanges)
                    await channel.ExchangeDeclareAsync(
                        exchange: "RestaurantService.API.IntegrationEvents:OrderRejectedEvent",
                        type: ExchangeType.Fanout,
                        durable: true,
                        autoDelete: false);
                    await channel.QueueBindAsync(
                        queue: SagaCommandQueue,
                        exchange: "RestaurantService.API.IntegrationEvents:OrderRejectedEvent",
                        routingKey: "");

                    await channel.ExchangeDeclareAsync(
                        exchange: "RestaurantService.API.IntegrationEvents:OrderApprovedEvent",
                        type: ExchangeType.Fanout,
                        durable: true,
                        autoDelete: false);
                    await channel.QueueBindAsync(
                        queue: SagaCommandQueue,
                        exchange: "RestaurantService.API.IntegrationEvents:OrderApprovedEvent",
                        routingKey: "");

                    _logger.LogInformation("[SagaWorker] ✓ Kuyruk hazır: {Queue}", SagaCommandQueue);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[SagaWorker] RabbitMQ henüz hazır değil, 5sn sonra tekrar denenecek...");
                    await Task.Delay(5000, stoppingToken);
                }
            }

            if (channel == null || stoppingToken.IsCancellationRequested)
                return;

            // Prefetch: bir seferde 1 mesaj işle (sıralı SAGA garantisi)
            await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

            // Consumer oluştur
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var routingKey = ea.RoutingKey;

                _logger.LogInformation("[SagaWorker] 📨 Komut alındı: {RoutingKey} | MessageId={MessageId}",
                    routingKey, ea.BasicProperties?.MessageId);

                try
                {
                    int maxRetries = 3;
                    for (int i = 0; i < maxRetries; i++)
                    {
                        try
                        {
                            await ProcessCommandAsync(routingKey, body);
                            break; // success
                        }
                        catch (Exception ex)
                        {
                            if (i == maxRetries - 1) throw; // throw on last attempt
                            _logger.LogWarning(ex, "[SagaWorker] Komut işleme hatası (Deneme {Attempt}/{MaxRetries}), tekrar deneniyor...", i + 1, maxRetries);
                            await Task.Delay(2000 * (i + 1));
                        }
                    }

                    // Başarılı — mesajı onayla
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);

                    _logger.LogInformation("[SagaWorker] ✓ Komut işlendi: {RoutingKey}", routingKey);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[SagaWorker] ✗ Komut işlenirken hata (Tüm denemeler başarısız): {RoutingKey}", routingKey);

                    // BasicNack ile reddet — requeue:false = Dead Letter Queue'ya düşer
                    // Kompanasyon zaten ProcessCommandAsync içinde yapılmıştır;
                    // mesaj DLQ'da manuel inceleme için bekler.
                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            // Kuyruğu dinlemeye başla
            await channel.BasicConsumeAsync(
                queue: SagaCommandQueue,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("[SagaWorker] ✓ Asenkron SAGA worker dinlemeye başladı. Kuyruk: {Queue}", SagaCommandQueue);

            // Uygulama kapanana kadar çalışmaya devam et
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        /// <summary>
        /// Gelen komutu routing key'e göre doğru SAGA adımına yönlendirir.
        /// Her komut bir scope içinde yürütülür (scoped servislere erişim için).
        /// </summary>
        private async Task ProcessCommandAsync(string routingKey, string messageBody)
        {
            // Önce MassTransit mesajı (Fanout) olup olmadığını kontrol et
            try
            {
                using var doc = JsonDocument.Parse(messageBody);
                if (doc.RootElement.TryGetProperty("messageType", out var msgTypeArray) && msgTypeArray.GetArrayLength() > 0)
                {
                    var msgType = msgTypeArray[0].GetString();
                    if (msgType != null)
                    {
                        if (msgType.Contains("OrderRejectedEvent"))
                        {
                            var msg = doc.RootElement.GetProperty("message");
                            var orderId = msg.GetProperty("orderId").GetString();
                            var reason = msg.TryGetProperty("reason", out var r) ? r.GetString() : "rejected_by_restaurant";
                            
                            using var scopeMT = _scopeFactory.CreateScope();
                            var orchMT = scopeMT.ServiceProvider.GetRequiredService<IOrderSagaOrchestrator>();
                            _logger.LogInformation("[SagaWorker] ❌ Restoran reddi işleniyor (MassTransit Event). OrderId={OrderId}", orderId);
                            await orchMT.HandleRestaurantRejectAsync(orderId, null, reason);
                            return;
                        }
                        else if (msgType.Contains("OrderApprovedEvent"))
                        {
                            var msg = doc.RootElement.GetProperty("message");
                            var orderId = msg.GetProperty("orderId").GetString();
                            
                            using var scopeMT = _scopeFactory.CreateScope();
                            var orchMT = scopeMT.ServiceProvider.GetRequiredService<IOrderSagaOrchestrator>();
                            _logger.LogInformation("[SagaWorker] ✅ Restoran onaylı işleniyor (MassTransit Event). OrderId={OrderId}", orderId);
                            await orchMT.HandleRestaurantConfirmAsync(orderId, null);
                            return;
                        }
                    }
                }
            }
            catch
            {
                // JsonDocument parse hatası veya MassTransit formatında değil, normal işleyişe devam et.
            }

            var command = JsonSerializer.Deserialize<SagaCommand>(messageBody, _jsonOpts);
            if (command == null || string.IsNullOrEmpty(command.CommandType))
            {
                _logger.LogWarning("[SagaWorker] Komut deserialize edilemedi veya CommandType boş.");
                return;
            }

            // Scoped servisler (OrderService, PaymentService vb.) için yeni scope oluştur
            using var scope = _scopeFactory.CreateScope();

            // SAGA bağlamını set et — BackgroundService'de HttpContext olmadığı için
            // auth bilgisini SagaCommand üzerinden taşıyoruz.
            var sagaContext = scope.ServiceProvider.GetRequiredService<ISagaContextAccessor>();
            sagaContext.AuthToken = command.AuthToken;
            sagaContext.UserId    = command.UserId;

            var orchestrator = scope.ServiceProvider.GetRequiredService<IOrderSagaOrchestrator>();

            switch (routingKey)
            {
                case CMD_START:
                    _logger.LogInformation("[SagaWorker] 🚀 SAGA başlatılıyor. UserId={UserId}", command.UserId);

                    var startPayload = command.Payload.HasValue
                        ? command.Payload.Value.GetRawText()
                        : "{}";
                    var startRequest = JsonSerializer.Deserialize<StartOrderSagaRequest>(startPayload, _jsonOpts);

                    await orchestrator.StartCheckoutSagaAsync(
                        command.CommandId, command.UserId, startRequest, command.IdempotencyKey);
                    break;

                case CMD_PAYMENT_CALLBACK:
                    _logger.LogInformation("[SagaWorker] 💳 Payment callback işleniyor. OrderId={OrderId}", command.OrderId);

                    var callbackPayload = command.Payload.HasValue
                        ? command.Payload.Value.GetRawText()
                        : "{}";
                    var callbackData = JsonSerializer.Deserialize<PaymentCallbackData>(callbackPayload, _jsonOpts);

                    await orchestrator.HandlePaymentCallbackAsync(
                        command.OrderId, callbackData?.PaymentId, callbackData?.Token);
                    break;

                case CMD_CONFIRM:
                    _logger.LogInformation("[SagaWorker] ✅ Restoran onaylı işleniyor. OrderId={OrderId}", command.OrderId);

                    var confirmPayload = command.Payload.HasValue
                        ? command.Payload.Value.GetRawText()
                        : "{}";
                    var confirmData = JsonSerializer.Deserialize<RestaurantActionData>(confirmPayload, _jsonOpts);

                    await orchestrator.HandleRestaurantConfirmAsync(
                        command.OrderId, confirmData?.RestaurantId);
                    break;

                case CMD_REJECT:
                    _logger.LogInformation("[SagaWorker] ❌ Restoran reddi işleniyor. OrderId={OrderId}", command.OrderId);

                    var rejectPayload = command.Payload.HasValue
                        ? command.Payload.Value.GetRawText()
                        : "{}";
                    var rejectData = JsonSerializer.Deserialize<RestaurantActionData>(rejectPayload, _jsonOpts);

                    await orchestrator.HandleRestaurantRejectAsync(
                        command.OrderId, rejectData?.RestaurantId, rejectData?.Reason ?? "restaurant_rejected");
                    break;

                case CMD_CANCEL:
                    _logger.LogInformation("[SagaWorker] 🚫 Müşteri iptali işleniyor. OrderId={OrderId}", command.OrderId);

                    await orchestrator.CancelSagaAsync(command.OrderId, command.UserId);
                    break;

                default:
                    _logger.LogWarning("[SagaWorker] Bilinmeyen komut tipi: {RoutingKey}", routingKey);
                    break;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SAGA Komut Modelleri
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// RabbitMQ kuyruğuna gönderilen SAGA komut mesajı.
    /// Controller tarafından oluşturulur, BackgroundService tarafından tüketilir.
    /// </summary>
    public class SagaCommand
    {
        [JsonPropertyName("commandId")]
        public string CommandId { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("commandType")]
        public string CommandType { get; set; }

        [JsonPropertyName("userId")]
        public string UserId { get; set; }

        [JsonPropertyName("orderId")]
        public string OrderId { get; set; }

        [JsonPropertyName("idempotencyKey")]
        public string IdempotencyKey { get; set; }

        /// <summary>
        /// Orijinal HTTP isteğindeki Authorization header değeri.
        /// BackgroundService'de HttpContext olmadığı için bu alan üzerinden taşınır.
        /// </summary>
        [JsonPropertyName("authToken")]
        public string AuthToken { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>Komuta özel veri (JSON element olarak saklanır)</summary>
        [JsonPropertyName("payload")]
        public JsonElement? Payload { get; set; }
    }

    /// <summary>Payment callback verisi</summary>
    public class PaymentCallbackData
    {
        [JsonPropertyName("paymentId")]
        public string PaymentId { get; set; }

        [JsonPropertyName("token")]
        public string Token { get; set; }
    }

    /// <summary>Restoran aksiyonu verisi</summary>
    public class RestaurantActionData
    {
        [JsonPropertyName("restaurantId")]
        public string RestaurantId { get; set; }

        [JsonPropertyName("reason")]
        public string Reason { get; set; }
    }
}
