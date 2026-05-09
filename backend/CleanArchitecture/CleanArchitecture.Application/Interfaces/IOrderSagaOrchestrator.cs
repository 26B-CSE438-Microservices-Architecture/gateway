using CleanArchitecture.Core.DTOs.Saga;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Interfaces
{
    /// <summary>
    /// Order SAGA Orchestrator — tüm sipariş yaşam döngüsünü yönetir.
    ///
    /// Choreography (ekipler arası) yerine Orchestrator pattern kullanılır:
    /// Bu servis, her adımı sırayla çağırır ve hata durumunda kompanasyon işlemlerini başlatır.
    ///
    /// SAGA Adımları:
    ///   1. Checkout (Order Service)          → Sipariş PAYMENT_PENDING durumuna geçer
    ///   2. Payment Init (Payment Service)    → iyzico checkout form başlatılır
    ///   3. Payment Callback                  → Kullanıcı formu doldurur, AUTHORIZED
    ///   4. Restaurant Confirm (Order Service)→ Restoran onaylar, Capture tetiklenir
    ///   5. Payment Capture (Payment Service) → Para çekilir
    ///
    /// Kompanasyon (Rollback) Akışı:
    ///   Adım 2'den sonra hata → Payment.Cancel (void)
    ///   Adım 3'ten sonra hata → Payment.Cancel (void) → Order.Cancel
    ///   Adım 4'ten sonra hata → Payment.Cancel (refund) → Order.Cancel
    /// </summary>
    public interface IOrderSagaOrchestrator
    {
        /// <summary>
        /// Yeni bir Order SAGA başlatır: sepeti siparişe dönüştürür ve ödeme formunu hazırlar.
        /// </summary>
        Task<SagaState> StartCheckoutSagaAsync(string userId, StartOrderSagaRequest request, string idempotencyKey);

        /// <summary>
        /// iyzico'dan dönen ödeme callback'ini işler ve SAGA'yı ilerletir.
        /// Başarıda Order Service'e HOLD_CONFIRMED callback'i gönderir.
        /// Başarısızlıkta siparişi iptal eder (kompanasyon).
        /// </summary>
        Task<SagaState> HandlePaymentCallbackAsync(string sagaId, string paymentId, string token);

        /// <summary>
        /// Restoran onayını işler: Order Service'e confirm isteği gönderir,
        /// ardından Payment Service'e capture isteği gönderir.
        /// </summary>
        Task<SagaState> HandleRestaurantConfirmAsync(string orderId, string restaurantId);

        /// <summary>
        /// Restoran reddetme işlemini işler: Order Service'e reject, Payment Service'e void.
        /// </summary>
        Task<SagaState> HandleRestaurantRejectAsync(string orderId, string restaurantId, string reason);

        /// <summary>
        /// Müşteri iptal isteğini işler: kompanasyon adımlarını başlatır.
        /// </summary>
        Task<SagaState> CancelSagaAsync(string orderId, string userId);

        /// <summary>
        /// Mevcut bir SAGA'nın durumunu sorgular.
        /// </summary>
        Task<SagaState> GetSagaStateAsync(string orderId);
    }
}
