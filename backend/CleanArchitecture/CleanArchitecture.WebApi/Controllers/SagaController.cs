using CleanArchitecture.Core.DTOs.Saga;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace CleanArchitecture.WebApi.Controllers
{
    /// <summary>
    /// Asenkron Order SAGA Orchestrator Controller
    ///
    /// Bu controller, SAGA komutlarını RabbitMQ kuyruğuna yayınlar ve
    /// hemen 202 Accepted döner. Komutlar arka planda SagaBackgroundService
    /// tarafından tüketilir ve SAGA adımları asenkron olarak yürütülür.
    ///
    /// Asenkron Akış:
    ///   1. Client → POST /saga/orders/start → 202 Accepted (sagaId döner)
    ///   2. BackgroundService komutu kuyruktan alır → SAGA adımlarını yürütür
    ///   3. Client → GET /saga/orders/{id} ile durumu sorgular (polling)
    ///
    /// SAGA Komutları (RabbitMQ routing keys):
    ///   saga.command.start              → Sipariş + Ödeme başlat
    ///   saga.command.payment-callback   → iyzico callback işle
    ///   saga.command.confirm            → Restoran onayı + Capture
    ///   saga.command.reject             → Restoran reddi + Void
    ///   saga.command.cancel             → Müşteri iptali + Compensate
    /// </summary>
    [Route("api/v1/saga/orders")]
    [ApiController]
    public class SagaController : ControllerBase
    {
        private readonly IOrderSagaOrchestrator _orchestrator;
        private readonly RabbitMqConnectionService _rabbitMq;
        private readonly IEventPublisher _eventPublisher;

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public SagaController(
            IOrderSagaOrchestrator orchestrator,
            RabbitMqConnectionService rabbitMq,
            IEventPublisher eventPublisher)
        {
            _orchestrator   = orchestrator;
            _rabbitMq       = rabbitMq;
            _eventPublisher = eventPublisher;
        }

        // ─── ADIM 1+2: Sipariş Başlat (ASENKRON) ──────────────────────────────

        /// <summary>
        /// Yeni bir Order SAGA başlatır (ASENKRON).
        ///
        /// Bu endpoint:
        /// 1. SAGA state'i oluşturur (status: NotStarted)
        /// 2. RabbitMQ kuyruğuna "saga.command.start" komutu yayınlar
        /// 3. Hemen 202 Accepted döner
        ///
        /// SAGA adımları arka planda SagaBackgroundService tarafından yürütülür.
        /// Client, GET /saga/orders/{orderId} ile durumu sorgulayabilir.
        /// </summary>
        [Authorize]
        [HttpPost("start")]
        public async Task<IActionResult> StartSaga(
            [FromBody] StartOrderSagaRequest request)
        {
            var userId = User.FindFirstValue("uid");
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var idempotencyKey = Request.Headers["Idempotency-Key"].ToString();
            if (string.IsNullOrEmpty(idempotencyKey))
                idempotencyKey = System.Guid.NewGuid().ToString();

            var sagaId = System.Guid.NewGuid().ToString();

            // RabbitMQ kuyruğuna SAGA başlatma komutu yayınla
            var command = new SagaCommand
            {
                CommandId      = sagaId,
                CommandType    = SagaBackgroundService.CMD_START,
                UserId         = userId,
                IdempotencyKey = idempotencyKey,
                Payload        = JsonSerializer.SerializeToElement(request, _jsonOpts)
            };

            await PublishCommandAsync(SagaBackgroundService.CMD_START, command);

            // 202 Accepted — "İsteğin alındı, arka planda işlenecek"
            return Accepted(new
            {
                sagaId,
                status = "QUEUED",
                message = "SAGA başlatma komutu kuyruğa eklendi. Durumu sorgulamak için GET /saga/orders/{orderId} kullanın.",
                pollingUrl = $"/api/v1/saga/orders/{sagaId}"
            });
        }

        // ─── ADIM 3: Ödeme Callback (ASENKRON) ─────────────────────────────────

        /// <summary>
        /// iyzico ödeme formundan dönen callback'i asenkron işler.
        /// Komutu RabbitMQ kuyruğuna yayınlar, 202 Accepted döner.
        /// </summary>
        [HttpPost("{orderId}/payment-callback/{paymentId}")]
        public async Task<IActionResult> PaymentCallback(
            string orderId,
            string paymentId,
            [FromBody] SagaPaymentCallbackRequest request)
        {
            var command = new SagaCommand
            {
                CommandType = SagaBackgroundService.CMD_PAYMENT_CALLBACK,
                OrderId     = orderId,
                Payload     = JsonSerializer.SerializeToElement(new PaymentCallbackData
                {
                    PaymentId = paymentId,
                    Token     = request.Token
                }, _jsonOpts)
            };

            await PublishCommandAsync(SagaBackgroundService.CMD_PAYMENT_CALLBACK, command);

            return Accepted(new
            {
                orderId,
                status = "QUEUED",
                message = "Payment callback komutu kuyruğa eklendi."
            });
        }

        // ─── ADIM 4+5: Restoran Onayı (ASENKRON) ──────────────────────────────

        /// <summary>
        /// Restoran siparişi onaylar — Komut kuyruğa eklenir, 202 döner.
        /// Arka planda: Order confirm + Payment capture yürütülür.
        /// </summary>
        [Authorize(Roles = "RestaurantOwner,restaurant_owner")]
        [HttpPost("{orderId}/confirm")]
        public async Task<IActionResult> ConfirmOrder(string orderId)
        {
            var restaurantId = User.FindFirstValue("restaurant_id");
            if (string.IsNullOrEmpty(restaurantId))
                return BadRequest(new { error = "RESTAURANT_NOT_ASSOCIATED", message = "User is not associated with any restaurant." });

            var command = new SagaCommand
            {
                CommandType = SagaBackgroundService.CMD_CONFIRM,
                OrderId     = orderId,
                Payload     = JsonSerializer.SerializeToElement(new RestaurantActionData
                {
                    RestaurantId = restaurantId
                }, _jsonOpts)
            };

            await PublishCommandAsync(SagaBackgroundService.CMD_CONFIRM, command);

            return Accepted(new
            {
                orderId,
                status = "QUEUED",
                message = "Restoran onay komutu kuyruğa eklendi."
            });
        }

        // ─── Restoran Reddi (ASENKRON) ─────────────────────────────────────────

        /// <summary>
        /// Restoran siparişi reddeder — Komut kuyruğa eklenir, 202 döner.
        /// Arka planda: void + order cancel yürütülür.
        /// </summary>
        [Authorize(Roles = "RestaurantOwner,restaurant_owner")]
        [HttpPost("{orderId}/reject")]
        public async Task<IActionResult> RejectOrder(
            string orderId,
            [FromBody] SagaRejectRequest request)
        {
            var restaurantId = User.FindFirstValue("restaurant_id");
            if (string.IsNullOrEmpty(restaurantId))
                return BadRequest(new { error = "RESTAURANT_NOT_ASSOCIATED", message = "User is not associated with any restaurant." });

            var command = new SagaCommand
            {
                CommandType = SagaBackgroundService.CMD_REJECT,
                OrderId     = orderId,
                Payload     = JsonSerializer.SerializeToElement(new RestaurantActionData
                {
                    RestaurantId = restaurantId,
                    Reason       = request?.Reason ?? "restaurant_rejected"
                }, _jsonOpts)
            };

            await PublishCommandAsync(SagaBackgroundService.CMD_REJECT, command);

            return Accepted(new
            {
                orderId,
                status = "QUEUED",
                message = "Restoran red komutu kuyruğa eklendi."
            });
        }

        // ─── Müşteri İptali (ASENKRON) ─────────────────────────────────────────

        /// <summary>
        /// Müşteri siparişi iptal eder — Komut kuyruğa eklenir, 202 döner.
        /// Arka planda: kompanasyon (void/refund + cancel) yürütülür.
        /// </summary>
        [Authorize]
        [HttpPost("{orderId}/cancel")]
        public async Task<IActionResult> CancelOrder(string orderId)
        {
            var userId = User.FindFirstValue("uid");
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var command = new SagaCommand
            {
                CommandType = SagaBackgroundService.CMD_CANCEL,
                OrderId     = orderId,
                UserId      = userId
            };

            await PublishCommandAsync(SagaBackgroundService.CMD_CANCEL, command);

            return Accepted(new
            {
                orderId,
                status = "QUEUED",
                message = "İptal komutu kuyruğa eklendi."
            });
        }

        // ─── SAGA Durum Sorgulama (SENKRON — polling endpoint) ─────────────────

        /// <summary>
        /// Bir SAGA'nın tüm adımlarının durumunu döndürür (polling endpoint).
        /// Client bu endpoint'i periyodik olarak çağırarak SAGA'nın ilerleyişini takip eder.
        /// </summary>
        [Authorize]
        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetSagaState(string orderId)
        {
            var saga = await _orchestrator.GetSagaStateAsync(orderId);
            if (saga == null)
                return NotFound(new { error = "SAGA_NOT_FOUND", message = $"No SAGA found for orderId={orderId}. SAGA may still be processing." });

            return Ok(saga);
        }

        // ─── Yardımcı: RabbitMQ'ya komut yayınla ──────────────────────────────

        private async Task PublishCommandAsync(string routingKey, SagaCommand command)
        {
            try
            {
                var json = JsonSerializer.Serialize(command, _jsonOpts);
                var body = Encoding.UTF8.GetBytes(json);

                var channel = await _rabbitMq.GetChannelAsync();

                var properties = new BasicProperties
                {
                    ContentType   = "application/json",
                    DeliveryMode  = DeliveryModes.Persistent,
                    MessageId     = command.CommandId,
                    Timestamp     = new AmqpTimestamp(System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                    Type          = routingKey
                };

                await channel.BasicPublishAsync(
                    exchange: RabbitMqConnectionService.ExchangeName,
                    routingKey: routingKey,
                    mandatory: false,
                    basicProperties: properties,
                    body: body);

                Serilog.Log.Information(
                    "[SagaController] 📤 Komut kuyruğa yayınlandı: {RoutingKey} | CommandId={CommandId}",
                    routingKey, command.CommandId);
            }
            catch (System.Exception ex)
            {
                Serilog.Log.Error(ex,
                    "[SagaController] ✗ Komut kuyruğa yayınlanamadı: {RoutingKey}. Hata: {Error}",
                    routingKey, ex.Message);
                throw; // Controller'da hata olursa 500 dönsün
            }
        }
    }

    // ─── Request DTO'ları ──────────────────────────────────────────────────────

    public class SagaPaymentCallbackRequest
    {
        public string Token { get; set; }
    }

    public class SagaRejectRequest
    {
        public string Reason { get; set; }
    }
}
