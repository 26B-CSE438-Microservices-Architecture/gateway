using System;
using System.Text.Json.Serialization;

namespace CleanArchitecture.Core.DTOs.Events
{
    /// <summary>
    /// Mikroservisler arası asenkron haberleşme için kullanılan integration event modeli.
    /// RabbitMQ üzerinden JSON olarak serialize edilip gönderilir.
    /// </summary>
    public class IntegrationEvent
    {
        /// <summary>Unique event ID — idempotency ve izleme için</summary>
        [JsonPropertyName("eventId")]
        public string EventId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Event tipi: "order.created", "payment.authorized", "saga.failed" vb.</summary>
        [JsonPropertyName("eventType")]
        public string EventType { get; set; }

        /// <summary>SAGA correlation ID — tüm adımları birbirine bağlar</summary>
        [JsonPropertyName("sagaId")]
        public string SagaId { get; set; }

        /// <summary>İlgili sipariş ID</summary>
        [JsonPropertyName("orderId")]
        public string OrderId { get; set; }

        /// <summary>İlgili kullanıcı ID</summary>
        [JsonPropertyName("userId")]
        public string UserId { get; set; }

        /// <summary>Event oluşturulma zamanı (UTC)</summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>Event'e özel ek veri (JSON serileştirilebilir herhangi bir obje)</summary>
        [JsonPropertyName("data")]
        public object Data { get; set; }

        /// <summary>Kaynak servis adı</summary>
        [JsonPropertyName("source")]
        public string Source { get; set; } = "gateway";
    }
}
