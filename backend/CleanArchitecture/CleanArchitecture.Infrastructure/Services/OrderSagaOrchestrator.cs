using CleanArchitecture.Core.DTOs.Order;
using CleanArchitecture.Core.DTOs.Payment;
using CleanArchitecture.Core.DTOs.Saga;
using CleanArchitecture.Core.DTOs.Events;
using CleanArchitecture.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Services
{
    /// <summary>
    /// Order SAGA Orchestrator implementasyonu.
    ///
    /// Bu sınıf, sipariş sürecindeki tüm mikroservis çağrılarını koordine eder.
    /// Hata durumunda compensating transaction'ları otomatik başlatır.
    /// Her adımın sonunda RabbitMQ üzerinden asenkron event yayınlar.
    ///
    /// SAGA State'leri in-memory Dictionary'de saklanır (production'da Redis/DB kullanılır).
    /// </summary>
    public class OrderSagaOrchestrator : IOrderSagaOrchestrator
    {
        private readonly IOrderService _orderService;
        private readonly IPaymentService _paymentService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OrderSagaOrchestrator> _logger;
        private readonly IEventPublisher _eventPublisher;

        // In-memory SAGA state store (key: orderId)
        // Production'da bu bir persistent store (Redis, DB) olmalıdır.
        private static readonly ConcurrentDictionary<string, SagaState> _sagaStore = new();

        // SAGA adım isimleri
        private const string STEP_CHECKOUT        = "Checkout";
        private const string STEP_PAYMENT_INIT    = "PaymentInitialization";
        private const string STEP_PAYMENT_AUTH    = "PaymentAuthorization";
        private const string STEP_RESTAURANT_CONF = "RestaurantConfirmation";
        private const string STEP_PAYMENT_CAPTURE = "PaymentCapture";

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public OrderSagaOrchestrator(
            IOrderService orderService,
            IPaymentService paymentService,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration,
            ILogger<OrderSagaOrchestrator> logger,
            IEventPublisher eventPublisher)
        {
            _orderService         = orderService;
            _paymentService       = paymentService;
            _httpContextAccessor  = httpContextAccessor;
            _configuration        = configuration;
            _logger               = logger;
            _eventPublisher       = eventPublisher;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // ADIM 1 + 2: Checkout → Payment Initialization
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// SAGA başlatır:
        ///   1. Order Service'e checkout isteği gönderir → sipariş oluşur (PAYMENT_PENDING)
        ///   2. Payment Service'e payment init isteği gönderir → checkout form hazırlanır
        /// Hata olursa:
        ///   - Adım 2 başarısız → Order Service'e cancel gönderir
        /// </summary>
        public async Task<SagaState> StartCheckoutSagaAsync(
            string sagaId,
            string userId,
            StartOrderSagaRequest request,
            string idempotencyKey)
        {
            var saga = new SagaState
            {
                SagaId    = sagaId,
                UserId    = userId,
                Status    = nameof(SagaStatus.OrderCreated),
                StartedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Steps     = InitializeSteps()
            };

            _logger.LogInformation("[SAGA:{SagaId}] Checkout SAGA başladı. UserId={UserId}", sagaId, userId);

            // Register early so polling can find it by sagaId
            _sagaStore[sagaId] = saga;

            // ── ADIM 1: Checkout ──────────────────────────────────────────────
            UpdateStep(saga, STEP_CHECKOUT, "InProgress");
            try
            {
                var checkoutReq = new CheckoutRequest
                {
                    DeliveryAddress = MapAddress(request.DeliveryAddress),
                    PaymentMethod   = request.PaymentMethod ?? "CREDIT_CARD",
                    OrderType       = request.OrderType ?? "DELIVERY",
                    Notes           = request.Notes
                };

                var order = await _orderService.CheckoutAsync(userId, checkoutReq, idempotencyKey);
                saga.OrderId     = order.OrderId;
                saga.TotalAmount = order.TotalAmount;
                saga.Currency    = order.Currency ?? "TRY";

                // Now that we have OrderId, store it with OrderId as key as well
                _sagaStore[saga.OrderId] = saga;
                UpdateStep(saga, STEP_CHECKOUT, "Success");
                saga.Status      = nameof(SagaStatus.OrderCreated);
                saga.CurrentStep = STEP_PAYMENT_INIT;

                _logger.LogInformation("[SAGA:{SagaId}] Adım 1 ✓ Sipariş oluştu. OrderId={OrderId}", sagaId, order.OrderId);

                // RabbitMQ: Sipariş oluşturuldu event'i yayınla
                await _eventPublisher.PublishAsync("order.created", saga.OrderId, sagaId, userId,
                    new { saga.TotalAmount, saga.Currency, status = "PAYMENT_PENDING" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SAGA:{SagaId}] Adım 1 ✗ Checkout başarısız.", sagaId);
                UpdateStep(saga, STEP_CHECKOUT, "Failed", ex.Message);
                saga.Status        = nameof(SagaStatus.Failed);
                saga.FailureReason = $"Checkout failed: {ex.Message}";
                saga.UpdatedAt     = DateTime.UtcNow;

                // RabbitMQ: SAGA başarısız event'i yayınla
                await _eventPublisher.PublishAsync("saga.failed", null, sagaId, userId,
                    new { step = STEP_CHECKOUT, reason = ex.Message });

                return saga;
            }

            // ── ADIM 2: Payment Initialization ────────────────────────────────
            UpdateStep(saga, STEP_PAYMENT_INIT, "InProgress");
            try
            {
                // Callback URL: ödeme formu tamamlandığında Gateway'e dönecek adres
                var callbackBase = _configuration["AppSettings:BaseUrl"]
                                ?? _configuration["ASPNETCORE_URLS"]?.Split(';')[0]
                                ?? "https://gw.cse.akdeniz.edu.tr/cse-438";
                var callbackUrl = request.CallbackUrl
                    ?? $"{callbackBase}/api/v1/saga/orders/{saga.OrderId}/payment-callback/{{paymentId}}";

                // Tutar: Payment Service minor unit (kuruş) bekliyor — TotalAmount * 100
                var amountInMinorUnits = (int)(saga.TotalAmount * 100);

                var paymentReq = new PaymentInitRequest
                {
                    OrderId       = saga.OrderId,
                    Amount        = amountInMinorUnits,
                    Currency      = saga.Currency,
                    PaymentMethod = "card",
                    CallbackUrl   = callbackUrl
                    // Buyer ve Items: ileride User Service'ten çekilebilir.
                    // Şimdilik Payment Service'in defaults'larına bırakıyoruz.
                };

                var paymentResult = await _paymentService.InitializePaymentAsync(
                    userId, paymentReq, $"{idempotencyKey}_pay");

                saga.PaymentId = paymentResult.Payment?.Id;

                if (paymentResult.CheckoutForm != null)
                {
                    saga.CheckoutForm = new SagaCheckoutForm
                    {
                        Token          = paymentResult.CheckoutForm.Token,
                        Content        = paymentResult.CheckoutForm.Content,
                        PaymentPageUrl = paymentResult.CheckoutForm.PaymentPageUrl
                    };
                }

                UpdateStep(saga, STEP_PAYMENT_INIT, "Success");
                saga.Status      = nameof(SagaStatus.PaymentInitiated);
                saga.CurrentStep = STEP_PAYMENT_AUTH;
                saga.UpdatedAt   = DateTime.UtcNow;

                _logger.LogInformation("[SAGA:{SagaId}] Adım 2 ✓ Ödeme başlatıldı. PaymentId={PaymentId}", sagaId, saga.PaymentId);

                // RabbitMQ: Ödeme başlatıldı event'i yayınla
                await _eventPublisher.PublishAsync("payment.initiated", saga.OrderId, sagaId, userId,
                    new { saga.PaymentId, saga.TotalAmount, saga.Currency });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SAGA:{SagaId}] Adım 2 ✗ Payment init başarısız. Kompanasyon başlıyor.", sagaId);
                UpdateStep(saga, STEP_PAYMENT_INIT, "Failed", ex.Message);
                saga.Status = nameof(SagaStatus.Compensating);

                // KOMPANASYON: Siparişi iptal et
                await CompensateCancelOrderAsync(saga, "payment_init_failed");

                saga.Status    = nameof(SagaStatus.Compensated);
                saga.UpdatedAt = DateTime.UtcNow;
                saga.FailureReason = $"Payment initialization failed: {ex.Message}";

                // RabbitMQ: Kompanasyon event'leri yayınla
                await _eventPublisher.PublishAsync("order.cancelled", saga.OrderId, sagaId, userId,
                    new { reason = "payment_init_failed", compensatedBy = "gateway" });
                await _eventPublisher.PublishAsync("saga.failed", saga.OrderId, sagaId, userId,
                    new { step = STEP_PAYMENT_INIT, reason = ex.Message });
            }

            _sagaStore[saga.OrderId] = saga;
            return saga;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // ADIM 3: Payment Callback (iyzico form tamamlandı)
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// iyzico'dan dönen token ile ödeme sonucunu sorgular.
        /// Başarıda: Order Service'e HOLD_CONFIRMED callback gönderir.
        /// Başarısızlıkta: Siparişi iptal eder (kompanasyon).
        /// </summary>
        public async Task<SagaState> HandlePaymentCallbackAsync(
            string sagaId, string paymentId, string token)
        {
            // SAGA state'ini bul (sagaId veya orderId ile)
            var saga = FindSagaByIdOrOrderId(sagaId);
            if (saga == null)
            {
                _logger.LogWarning("[SAGA] Bulunamadı: {Key}", sagaId);
                return null;
            }

            UpdateStep(saga, STEP_PAYMENT_AUTH, "InProgress");

            try
            {
                // Payment Service'e callback ilet
                var paymentResult = await _paymentService.ProcessCallbackAsync(paymentId, token);

                if (paymentResult?.Status == "AUTHORIZED")
                {
                    UpdateStep(saga, STEP_PAYMENT_AUTH, "Success");
                    saga.Status      = nameof(SagaStatus.PaymentAuthorized);
                    saga.CurrentStep = STEP_RESTAURANT_CONF;
                    saga.UpdatedAt   = DateTime.UtcNow;

                    // Order Service'e HOLD_CONFIRMED bildir
                    await _orderService.ProcessPaymentCallbackAsync(saga.OrderId, "HOLD_CONFIRMED");

                    _logger.LogInformation("[SAGA:{SagaId}] Adım 3 ✓ Ödeme authorize edildi. Para bloke edildi.", saga.SagaId);

                    // RabbitMQ: Ödeme authorize event'i yayınla
                    await _eventPublisher.PublishAsync("payment.authorized", saga.OrderId, saga.SagaId, saga.UserId,
                        new { saga.PaymentId, status = "AUTHORIZED" });
                }
                else
                {
                    // Ödeme başarısız → kompanasyon
                    var failReason = paymentResult?.FailureReason ?? paymentResult?.Status ?? "unknown";
                    _logger.LogWarning("[SAGA:{SagaId}] Adım 3 ✗ Ödeme başarısız: {Reason}. Kompanasyon başlıyor.", saga.SagaId, failReason);

                    UpdateStep(saga, STEP_PAYMENT_AUTH, "Failed", failReason);
                    saga.Status = nameof(SagaStatus.Compensating);

                    // Order Service'e HOLD_FAILED bildir
                    await _orderService.ProcessPaymentCallbackAsync(saga.OrderId, "HOLD_FAILED");
                    await CompensateCancelOrderAsync(saga, "payment_authorization_failed");

                    saga.Status        = nameof(SagaStatus.Compensated);
                    saga.FailureReason = $"Payment authorization failed: {failReason}";

                    // RabbitMQ: Kompanasyon event'leri yayınla
                    await _eventPublisher.PublishAsync("payment.cancelled", saga.OrderId, saga.SagaId, saga.UserId,
                        new { saga.PaymentId, reason = "authorization_failed" });
                    await _eventPublisher.PublishAsync("order.cancelled", saga.OrderId, saga.SagaId, saga.UserId,
                        new { reason = "payment_authorization_failed", compensatedBy = "gateway" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SAGA:{SagaId}] Adım 3 ✗ Callback işleme hatası.", saga.SagaId);
                UpdateStep(saga, STEP_PAYMENT_AUTH, "Failed", ex.Message);
                saga.Status        = nameof(SagaStatus.Failed);
                saga.FailureReason = $"Payment callback processing error: {ex.Message}";
            }

            saga.UpdatedAt = DateTime.UtcNow;
            _sagaStore[saga.OrderId] = saga;
            return saga;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // ADIM 4 + 5: Restaurant Confirm → Payment Capture
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Restoran onayını işler:
        ///   4. Order Service'e confirm gönderir
        ///   5. Payment Service'e capture gönderir → para çekilir
        /// Hata olursa:
        ///   - Payment void → Order cancel (kompanasyon)
        /// </summary>
        public async Task<SagaState> HandleRestaurantConfirmAsync(
            string orderId, string restaurantId)
        {
            var saga = GetSagaByOrderId(orderId);
            if (saga == null)
            {
                // SAGA state yoksa senkron proxy gibi davran
                saga = CreateMinimalSaga(orderId, null);
            }

            // ── ADIM 4: Restoran Onayı ─────────────────────────────────────────
            UpdateStep(saga, STEP_RESTAURANT_CONF, "InProgress");
            try
            {
                var confirmedOrder = await _orderService.ConfirmOrderAsync(restaurantId, orderId);
                UpdateStep(saga, STEP_RESTAURANT_CONF, "Success");
                saga.Status      = nameof(SagaStatus.RestaurantConfirmed);
                saga.CurrentStep = STEP_PAYMENT_CAPTURE;

                _logger.LogInformation("[SAGA:{SagaId}] Adım 4 ✓ Restoran onayladı. OrderId={OrderId}", saga.SagaId, orderId);

                // RabbitMQ: Restoran onay event'i yayınla
                await _eventPublisher.PublishAsync("restaurant.confirmed", orderId, saga.SagaId, saga.UserId,
                    new { restaurantId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SAGA:{SagaId}] Adım 4 ✗ Restoran onayı başarısız.", saga.SagaId);
                UpdateStep(saga, STEP_RESTAURANT_CONF, "Failed", ex.Message);
                saga.Status        = nameof(SagaStatus.Failed);
                saga.FailureReason = $"Restaurant confirmation failed: {ex.Message}";
                saga.UpdatedAt     = DateTime.UtcNow;
                _sagaStore[orderId] = saga;
                return saga;
            }

            // ── ADIM 5: Payment Capture ────────────────────────────────────────
            UpdateStep(saga, STEP_PAYMENT_CAPTURE, "InProgress");
            try
            {
                if (!string.IsNullOrEmpty(saga.PaymentId))
                {
                    var amountInMinorUnits = (int)(saga.TotalAmount * 100);
                    await _paymentService.CapturePaymentAsync(saga.PaymentId, amountInMinorUnits);

                    // Order Service'e CAPTURE_COMPLETED bildir
                    await _orderService.ProcessPaymentCallbackAsync(orderId, "CAPTURE_COMPLETED");
                }
                else
                {
                    // PaymentId yoksa (state kaybı / minimal SAGA): capture edilemez, kompanasyon gerekli
                    _logger.LogWarning("[SAGA:{SagaId}] Adım 5 ✗ PaymentId bulunamadı — capture atlanamaz. Kompanasyon başlıyor.", saga.SagaId);
                    throw new InvalidOperationException("PaymentId is missing — cannot capture payment.");
                }

                UpdateStep(saga, STEP_PAYMENT_CAPTURE, "Success");
                saga.Status      = nameof(SagaStatus.PaymentCaptured);
                saga.CurrentStep = null; // Tüm adımlar tamamlandı

                _logger.LogInformation("[SAGA:{SagaId}] Adım 5 ✓ Para çekildi. SAGA tamamlandı.", saga.SagaId);

                // RabbitMQ: Ödeme çekildi ve sipariş tamamlandı event'leri yayınla
                await _eventPublisher.PublishAsync("payment.captured", orderId, saga.SagaId, saga.UserId,
                    new { saga.PaymentId, saga.TotalAmount });
                await _eventPublisher.PublishAsync("order.completed", orderId, saga.SagaId, saga.UserId,
                    new { saga.TotalAmount, saga.Currency });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SAGA:{SagaId}] Adım 5 ✗ Capture başarısız. Kompanasyon başlıyor.", saga.SagaId);
                UpdateStep(saga, STEP_PAYMENT_CAPTURE, "Failed", ex.Message);
                saga.Status = nameof(SagaStatus.Compensating);

                // KOMPANASYON: Capture başarısız → para iade et → siparişi iptal et
                await CompensateVoidPaymentAsync(saga, "capture_failed");
                await CompensateCancelOrderAsync(saga, "capture_failed");

                // Order Service'e CAPTURE_FAILED bildir
                try { await _orderService.ProcessPaymentCallbackAsync(orderId, "CAPTURE_FAILED"); } catch { }

                saga.Status        = nameof(SagaStatus.Compensated);
                saga.FailureReason = $"Payment capture failed: {ex.Message}";

                // RabbitMQ: Kompanasyon event'leri yayınla
                await _eventPublisher.PublishAsync("payment.cancelled", orderId, saga.SagaId, saga.UserId,
                    new { saga.PaymentId, reason = "capture_failed" });
                await _eventPublisher.PublishAsync("order.cancelled", orderId, saga.SagaId, saga.UserId,
                    new { reason = "capture_failed", compensatedBy = "gateway" });
            }

            saga.UpdatedAt = DateTime.UtcNow;
            _sagaStore[orderId] = saga;
            return saga;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // KOMPANASYON: Restoran Reddi
        // ═══════════════════════════════════════════════════════════════════════

        public async Task<SagaState> HandleRestaurantRejectAsync(
            string orderId, string restaurantId, string reason)
        {
            var saga = GetSagaByOrderId(orderId) ?? CreateMinimalSaga(orderId, null);

            _logger.LogInformation("[SAGA:{SagaId}] Restoran reddetti. Kompanasyon başlıyor. Reason={Reason}", saga.SagaId, reason);
            saga.Status = nameof(SagaStatus.Compensating);

            try
            {
                // Order Service'e reject gönder
                await _orderService.RejectOrderAsync(restaurantId, orderId);
                UpdateStep(saga, STEP_RESTAURANT_CONF, "Compensated",
                    compensationAction: "order.reject");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[SAGA:{SagaId}] Order reject başarısız (zaten iptal olmuş olabilir).", saga.SagaId);
            }

            // Para tutulmuşsa void et
            if (!string.IsNullOrEmpty(saga.PaymentId))
            {
                await CompensateVoidPaymentAsync(saga, reason);
            }

            saga.Status        = nameof(SagaStatus.Compensated);
            saga.FailureReason = $"Restaurant rejected: {reason}";
            saga.UpdatedAt     = DateTime.UtcNow;

            // RabbitMQ: Restoran reddi kompanasyon event'leri yayınla
            await _eventPublisher.PublishAsync("restaurant.rejected", orderId, saga.SagaId, saga.UserId,
                new { restaurantId, reason });
            await _eventPublisher.PublishAsync("order.cancelled", orderId, saga.SagaId, saga.UserId,
                new { reason = $"restaurant_rejected: {reason}", compensatedBy = "gateway" });
            if (!string.IsNullOrEmpty(saga.PaymentId))
            {
                await _eventPublisher.PublishAsync("payment.cancelled", orderId, saga.SagaId, saga.UserId,
                    new { saga.PaymentId, reason = "restaurant_rejected" });
            }
            _sagaStore[orderId] = saga;
            return saga;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // KOMPANASYON: Müşteri İptali
        // ═══════════════════════════════════════════════════════════════════════

        public async Task<SagaState> CancelSagaAsync(string orderId, string userId)
        {
            var saga = GetSagaByOrderId(orderId) ?? CreateMinimalSaga(orderId, userId);

            _logger.LogInformation("[SAGA:{SagaId}] Müşteri iptal isteği. OrderId={OrderId}", saga.SagaId, orderId);
            saga.Status = nameof(SagaStatus.Compensating);

            // Para authorize/capture edilmişse iade et
            if (!string.IsNullOrEmpty(saga.PaymentId))
            {
                await CompensateVoidPaymentAsync(saga, "customer_cancel");
            }

            // Siparişi iptal et
            await CompensateCancelOrderAsync(saga, "customer_cancel");

            saga.Status        = nameof(SagaStatus.Compensated);
            saga.FailureReason = "Cancelled by customer";
            saga.UpdatedAt     = DateTime.UtcNow;

            // RabbitMQ: Müşteri iptali kompanasyon event'leri yayınla
            await _eventPublisher.PublishAsync("order.cancelled", orderId, saga.SagaId, userId,
                new { reason = "customer_cancel", compensatedBy = "gateway" });
            if (!string.IsNullOrEmpty(saga.PaymentId))
            {
                await _eventPublisher.PublishAsync("payment.cancelled", orderId, saga.SagaId, userId,
                    new { saga.PaymentId, reason = "customer_cancel" });
            }
            _sagaStore[orderId] = saga;
            return saga;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // DURUM SORGULAMA
        // ═══════════════════════════════════════════════════════════════════════

        public Task<SagaState> GetSagaStateAsync(string key)
        {
            // Önce orderId olarak ara
            if (_sagaStore.TryGetValue(key, out var saga)) return Task.FromResult(saga);
            // Sonra sagaId olarak ara (SAGA başlar başlamaz orderId henüz yoktur)
            var bySagaId = _sagaStore.Values.FirstOrDefault(s => s.SagaId == key);
            return Task.FromResult(bySagaId);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // YARDIMCI METODLAR — Kompanasyon
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Payment'ı void veya refund eder (durumuna göre Payment Service karar verir).
        /// </summary>
        private async Task CompensateVoidPaymentAsync(SagaState saga, string reason)
        {
            var paymentIdToCancel = saga.PaymentId;

            if (string.IsNullOrEmpty(paymentIdToCancel))
            {
                try
                {
                    var payments = await _paymentService.GetPaymentsByOrderIdAsync(saga.OrderId);
                    if (payments != null && payments.Count > 0)
                    {
                        // En güncel payment id'yi alıyoruz (genelde ilk veya tek kayıttır)
                        paymentIdToCancel = payments[0].Id;
                        saga.PaymentId = paymentIdToCancel;
                        _logger.LogInformation("[SAGA:{SagaId}] PaymentId hafızada yoktu, Payment Service'ten bulundu: {PaymentId}", saga.SagaId, paymentIdToCancel);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[SAGA:{SagaId}] Ödeme servisinde {OrderId} için ödeme aranırken hata oluştu.", saga.SagaId, saga.OrderId);
                }
            }

            if (string.IsNullOrEmpty(paymentIdToCancel)) return;

            try
            {
                await _paymentService.CancelPaymentAsync(paymentIdToCancel, reason);
                UpdateStep(saga, STEP_PAYMENT_INIT, "Compensated",
                    compensationAction: "payment.cancel",
                    compensationStatus: "Success");

                _logger.LogInformation("[SAGA:{SagaId}] Kompanasyon ✓ Ödeme iptal/iade edildi. PaymentId={PaymentId}",
                    saga.SagaId, paymentIdToCancel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SAGA:{SagaId}] Kompanasyon ✗ Ödeme iptali başarısız! Manuel müdahale gerekiyor. PaymentId={PaymentId}",
                    saga.SagaId, paymentIdToCancel);
                UpdateStep(saga, STEP_PAYMENT_INIT, "Compensated",
                    compensationAction: "payment.cancel",
                    compensationStatus: "Failed");
            }
        }

        /// <summary>
        /// Order Service'e sipariş iptali gönderir.
        /// </summary>
        private async Task CompensateCancelOrderAsync(SagaState saga, string reason)
        {
            if (string.IsNullOrEmpty(saga.OrderId)) return;

            try
            {
                await _orderService.CancelOrderAsync(saga.UserId, saga.OrderId);
                UpdateStep(saga, STEP_CHECKOUT, "Compensated",
                    compensationAction: "order.cancel",
                    compensationStatus: "Success");

                _logger.LogInformation("[SAGA:{SagaId}] Kompanasyon ✓ Sipariş iptal edildi. OrderId={OrderId}",
                    saga.SagaId, saga.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[SAGA:{SagaId}] Kompanasyon ✗ Sipariş iptali başarısız (zaten iptal edilmiş olabilir). OrderId={OrderId}",
                    saga.SagaId, saga.OrderId);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // YARDIMCI METODLAR — Yardımcılar
        // ═══════════════════════════════════════════════════════════════════════

        private static List<SagaStep> InitializeSteps() => new()
        {
            new SagaStep { StepName = STEP_CHECKOUT,        Status = "Pending", CompensationAction = "order.cancel" },
            new SagaStep { StepName = STEP_PAYMENT_INIT,    Status = "Pending", CompensationAction = "payment.cancel" },
            new SagaStep { StepName = STEP_PAYMENT_AUTH,    Status = "Pending", CompensationAction = "payment.cancel" },
            new SagaStep { StepName = STEP_RESTAURANT_CONF, Status = "Pending", CompensationAction = "order.reject" },
            new SagaStep { StepName = STEP_PAYMENT_CAPTURE, Status = "Pending", CompensationAction = "payment.refund" },
        };

        private static void UpdateStep(
            SagaState saga,
            string stepName,
            string status,
            string errorMessage = null,
            string compensationAction = null,
            string compensationStatus = null)
        {
            var step = saga.Steps.Find(s => s.StepName == stepName);
            if (step == null) return;

            step.Status = status;
            if (status == "InProgress") step.StartedAt = DateTime.UtcNow;
            if (status is "Success" or "Failed" or "Compensated") step.CompletedAt = DateTime.UtcNow;
            if (errorMessage != null) step.ErrorMessage = errorMessage;
            if (compensationAction != null) step.CompensationAction = compensationAction;
            if (compensationStatus != null) step.CompensationStatus = compensationStatus;
        }

        private SagaState GetSagaByOrderId(string orderId)
        {
            _sagaStore.TryGetValue(orderId, out var saga);
            return saga;
        }

        private SagaState FindSagaByIdOrOrderId(string key)
        {
            // Önce orderId olarak ara
            if (_sagaStore.TryGetValue(key, out var saga)) return saga;
            // Sonra sagaId olarak ara
            foreach (var s in _sagaStore.Values)
                if (s.SagaId == key) return s;
            return null;
        }

        private static SagaState CreateMinimalSaga(string orderId, string userId) => new()
        {
            SagaId    = Guid.NewGuid().ToString(),
            OrderId   = orderId,
            UserId    = userId,
            Status    = nameof(SagaStatus.OrderCreated),
            StartedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Steps     = InitializeSteps()
        };

        private static OrderAddressDto MapAddress(SagaDeliveryAddress addr)
        {
            if (addr == null) return null;
            return new OrderAddressDto
            {
                Street     = addr.Street,
                District   = addr.District,
                City       = addr.City,
                PostalCode = addr.PostalCode,
                Lat        = addr.Lat,
                Lng        = addr.Lng
            };
        }
    }
}
