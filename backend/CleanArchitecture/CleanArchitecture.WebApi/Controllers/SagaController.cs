using CleanArchitecture.Core.DTOs.Saga;
using CleanArchitecture.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Controllers
{
    /// <summary>
    /// Order SAGA Orchestrator Controller
    ///
    /// Bu controller, sipariş SAGA'sının tüm yaşam döngüsünü yönetir.
    /// Klasik proxy endpoint'lerinin aksine, her istek birden fazla
    /// mikroservisi koordineli olarak çağırır ve hata durumunda
    /// compensating transaction'lar başlatır.
    ///
    /// SAGA Akışı:
    ///   POST /saga/orders/start       → Sipariş + Ödeme başlat
    ///   POST /saga/orders/{id}/payment-callback/{paymentId} → iyzico callback
    ///   POST /saga/orders/{id}/confirm → Restoran onayı + Capture
    ///   POST /saga/orders/{id}/reject  → Restoran reddi + Void
    ///   POST /saga/orders/{id}/cancel  → Müşteri iptali + Compensate
    ///   GET  /saga/orders/{id}         → SAGA durumunu sorgula
    /// </summary>
    [Route("api/v1/saga/orders")]
    [ApiController]
    public class SagaController : ControllerBase
    {
        private readonly IOrderSagaOrchestrator _orchestrator;

        public SagaController(IOrderSagaOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;
        }

        // ─── ADIM 1+2: Sipariş Başlat ─────────────────────────────────────────

        /// <summary>
        /// Yeni bir Order SAGA başlatır.
        ///
        /// Bu endpoint şunları sırayla yapar:
        /// 1. Order Service'e checkout isteği gönderir (sipariş oluşturur)
        /// 2. Payment Service'e ödeme başlatma isteği gönderir (checkout form hazırlar)
        ///
        /// Hata durumunda: Oluşan sipariş otomatik iptal edilir.
        ///
        /// Response: SAGA state + iyzico checkout form içeriği
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

            var saga = await _orchestrator.StartCheckoutSagaAsync(userId, request, idempotencyKey);

            if (saga.Status == nameof(SagaStatus.Failed) || saga.Status == nameof(SagaStatus.Compensated))
                return UnprocessableEntity(saga);

            return Ok(saga);
        }

        // ─── ADIM 3: Ödeme Callback ────────────────────────────────────────────

        /// <summary>
        /// iyzico ödeme formundan dönen callback'i işler.
        ///
        /// Bu endpoint şunları yapar:
        /// 1. Payment Service'e token ile checkout form sonucunu sorgular
        /// 2. Başarıda Order Service'e HOLD_CONFIRMED bildirir
        /// 3. Başarısızlıkta siparişi iptal eder (HOLD_FAILED + order cancel)
        ///
        /// NOT: Bu endpoint Auth gerektirmez — iyzico'dan gelen redirect.
        /// </summary>
        [HttpPost("{orderId}/payment-callback/{paymentId}")]
        public async Task<IActionResult> PaymentCallback(
            string orderId,
            string paymentId,
            [FromBody] SagaPaymentCallbackRequest request)
        {
            var saga = await _orchestrator.HandlePaymentCallbackAsync(orderId, paymentId, request.Token);

            if (saga == null) return NotFound(new { error = "SAGA_NOT_FOUND", message = $"No SAGA found for orderId={orderId}" });

            if (saga.Status == nameof(SagaStatus.PaymentAuthorized))
                return Ok(saga);

            return UnprocessableEntity(saga);
        }

        // ─── ADIM 4+5: Restoran Onayı ─────────────────────────────────────────

        /// <summary>
        /// Restoran siparişi onaylar — Capture akışını tetikler.
        ///
        /// Bu endpoint şunları sırayla yapar:
        /// 1. Order Service'e confirm gönderir
        /// 2. Payment Service'e capture gönderir (para çekilir)
        ///
        /// Hata durumunda: Payment void edilir, sipariş iptal edilir.
        /// </summary>
        [Authorize(Roles = "RestaurantOwner,restaurant_owner")]
        [HttpPost("{orderId}/confirm")]
        public async Task<IActionResult> ConfirmOrder(string orderId)
        {
            var restaurantId = User.FindFirstValue("restaurant_id");
            if (string.IsNullOrEmpty(restaurantId))
                return BadRequest(new { error = "RESTAURANT_NOT_ASSOCIATED", message = "User is not associated with any restaurant." });

            var saga = await _orchestrator.HandleRestaurantConfirmAsync(orderId, restaurantId);

            if (saga.Status == nameof(SagaStatus.PaymentCaptured))
                return Ok(saga);

            return UnprocessableEntity(saga);
        }

        // ─── Restoran Reddi ────────────────────────────────────────────────────

        /// <summary>
        /// Restoran siparişi reddeder — Hold release (void) başlatır.
        ///
        /// Bu endpoint şunları yapar:
        /// 1. Order Service'e reject gönderir
        /// 2. Payment Service'e void gönderir (para serbest bırakılır)
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

            var saga = await _orchestrator.HandleRestaurantRejectAsync(orderId, restaurantId, request?.Reason ?? "restaurant_rejected");

            return Ok(saga);
        }

        // ─── Müşteri İptali ────────────────────────────────────────────────────

        /// <summary>
        /// Müşteri siparişi iptal eder — Compensating transaction başlatır.
        ///
        /// Bu endpoint şunları yapar:
        /// 1. Ödeme durumuna göre void veya refund başlatır
        /// 2. Order Service'e cancel gönderir
        ///
        /// Sipariş hangi aşamada olursa olsun doğru kompanasyon çalışır.
        /// </summary>
        [Authorize]
        [HttpPost("{orderId}/cancel")]
        public async Task<IActionResult> CancelOrder(string orderId)
        {
            var userId = User.FindFirstValue("uid");
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var saga = await _orchestrator.CancelSagaAsync(orderId, userId);
            return Ok(saga);
        }

        // ─── SAGA Durum Sorgulama ──────────────────────────────────────────────

        /// <summary>
        /// Bir SAGA'nın tüm adımlarının durumunu döndürür.
        ///
        /// Her adımın başarı/başarısızlık durumu, kompanasyon işlemleri
        /// ve zaman damgaları dahil tüm bilgiler döner.
        /// </summary>
        [Authorize]
        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetSagaState(string orderId)
        {
            var saga = await _orchestrator.GetSagaStateAsync(orderId);
            if (saga == null)
                return NotFound(new { error = "SAGA_NOT_FOUND", message = $"No SAGA found for orderId={orderId}" });

            return Ok(saga);
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
