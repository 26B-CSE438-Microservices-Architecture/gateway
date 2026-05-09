using CleanArchitecture.Core.DTOs.Events;
using CleanArchitecture.Core.Interfaces;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Services
{
    /// <summary>
    /// RabbitMQ üzerinden integration event yayınlayan servis.
    /// 
    /// - Exchange: gateway.events (topic)
    /// - Routing key: event tipi (örn: "order.created", "payment.authorized")
    /// - Mesaj: IntegrationEvent JSON serialize edilmiş hali
    /// 
    /// ÖNEMLİ: RabbitMQ'ya bağlanılamasa veya mesaj gönderilemese bile
    /// exception fırlatmaz — sadece loglar. Böylece RabbitMQ çökse bile
    /// SAGA akışı (HTTP tabanlı) çalışmaya devam eder.
    /// </summary>
    public class RabbitMqEventPublisher : IEventPublisher
    {
        private readonly RabbitMqConnectionService _connectionService;
        private readonly ILogger<RabbitMqEventPublisher> _logger;

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        public RabbitMqEventPublisher(
            RabbitMqConnectionService connectionService,
            ILogger<RabbitMqEventPublisher> logger)
        {
            _connectionService = connectionService;
            _logger = logger;
        }

        /// <summary>
        /// RabbitMQ'ya bir integration event yayınlar.
        /// Hata durumunda exception fırlatmaz (fire-and-forget).
        /// </summary>
        public async Task PublishAsync(
            string eventType,
            string orderId,
            string sagaId,
            string userId,
            object data = null)
        {
            try
            {
                var integrationEvent = new IntegrationEvent
                {
                    EventId   = Guid.NewGuid().ToString(),
                    EventType = eventType,
                    OrderId   = orderId,
                    SagaId    = sagaId,
                    UserId    = userId,
                    Timestamp = DateTime.UtcNow,
                    Data      = data,
                    Source    = "gateway"
                };

                var json = JsonSerializer.Serialize(integrationEvent, _jsonOpts);
                var body = Encoding.UTF8.GetBytes(json);

                var properties = new BasicProperties
                {
                    ContentType  = "application/json",
                    DeliveryMode = DeliveryModes.Persistent,
                    MessageId    = integrationEvent.EventId,
                    Timestamp    = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                    Type         = eventType,
                    CorrelationId = sagaId
                };

                // Thread-safe publish — paylaşılan channel üzerinde race condition’u önler
                await _connectionService.PublishAsync(eventType, properties, body);

                _logger.LogInformation(
                    "[RabbitMQ] ✓ Event yayınlandı: {EventType} | OrderId={OrderId} | SagaId={SagaId}",
                    eventType, orderId, sagaId);
            }
            catch (Exception ex)
            {
                // RabbitMQ hatası SAGA akışını BOZMAMALI
                _logger.LogWarning(ex,
                    "[RabbitMQ] ✗ Event yayınlanamadı: {EventType} | OrderId={OrderId} | Hata: {Error}",
                    eventType, orderId, ex.Message);
            }
        }
    }
}
