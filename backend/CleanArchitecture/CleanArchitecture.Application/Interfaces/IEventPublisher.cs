using System.Threading.Tasks;

namespace CleanArchitecture.Core.Interfaces
{
    /// <summary>
    /// Asenkron event yayınlama arayüzü.
    /// RabbitMQ üzerinden integration event'leri publish eder.
    /// 
    /// Kullanım: SAGA Orchestrator her adımın sonunda ilgili event'i yayınlar.
    /// Hata durumunda kompanasyon event'leri yayınlanır.
    /// </summary>
    public interface IEventPublisher
    {
        /// <summary>
        /// Belirtilen event tipinde bir integration event yayınlar.
        /// </summary>
        /// <param name="eventType">Event tipi (routing key olarak kullanılır). Örn: "order.created"</param>
        /// <param name="orderId">İlgili sipariş ID</param>
        /// <param name="sagaId">SAGA correlation ID</param>
        /// <param name="userId">İlgili kullanıcı ID</param>
        /// <param name="data">Event'e özel ek veri</param>
        Task PublishAsync(string eventType, string orderId, string sagaId, string userId, object data = null);
    }
}
