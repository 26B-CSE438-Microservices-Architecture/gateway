using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CleanArchitecture.Core.DTOs.Saga
{
    // ─── SAGA State Enum ────────────────────────────────────────────────────────

    public enum SagaStatus
    {
        /// <summary>SAGA henüz başlamadı</summary>
        NotStarted,
        /// <summary>Sipariş oluşturuldu, ödeme bekleniyor</summary>
        OrderCreated,
        /// <summary>Ödeme başlatıldı, kullanıcı formu dolduruyor</summary>
        PaymentInitiated,
        /// <summary>Ödeme authorize edildi (para bloke), restoran onayı bekleniyor</summary>
        PaymentAuthorized,
        /// <summary>Restoran onayladı, para çekme işlemi başladı</summary>
        RestaurantConfirmed,
        /// <summary>Para çekildi, sipariş hazırlanıyor</summary>
        PaymentCaptured,
        /// <summary>Sipariş teslim edildi — başarıyla tamamlandı</summary>
        Completed,
        /// <summary>SAGA başarısız oldu — tüm adımlar kompanse edildi</summary>
        Failed,
        /// <summary>Kompanasyon işlemleri devam ediyor (rollback)</summary>
        Compensating,
        /// <summary>Kompanasyon tamamlandı</summary>
        Compensated
    }

    // ─── Bireysel SAGA Adım Modeli ─────────────────────────────────────────────

    public class SagaStep
    {
        [JsonPropertyName("stepName")]
        public string StepName { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }  // "Pending" | "Success" | "Failed" | "Compensated"

        [JsonPropertyName("startedAt")]
        public DateTime? StartedAt { get; set; }

        [JsonPropertyName("completedAt")]
        public DateTime? CompletedAt { get; set; }

        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; set; }

        [JsonPropertyName("compensationAction")]
        public string CompensationAction { get; set; }

        [JsonPropertyName("compensationStatus")]
        public string CompensationStatus { get; set; }
    }

    // ─── SAGA İzleme Kaydı ─────────────────────────────────────────────────────

    public class SagaState
    {
        [JsonPropertyName("sagaId")]
        public string SagaId { get; set; }

        [JsonPropertyName("orderId")]
        public string OrderId { get; set; }

        [JsonPropertyName("userId")]
        public string UserId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("currentStep")]
        public string CurrentStep { get; set; }

        [JsonPropertyName("steps")]
        public List<SagaStep> Steps { get; set; } = new();

        [JsonPropertyName("paymentId")]
        public string PaymentId { get; set; }

        [JsonPropertyName("checkoutForm")]
        public SagaCheckoutForm CheckoutForm { get; set; }

        [JsonPropertyName("totalAmount")]
        public double TotalAmount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "TRY";

        [JsonPropertyName("startedAt")]
        public DateTime StartedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("failureReason")]
        public string FailureReason { get; set; }
    }

    public class SagaCheckoutForm
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("paymentPageUrl")]
        public string PaymentPageUrl { get; set; }
    }

    // ─── SAGA Başlatma İsteği ──────────────────────────────────────────────────

    public class StartOrderSagaRequest
    {
        /// <summary>
        /// Ödeme yapılacak adres
        /// </summary>
        [JsonPropertyName("deliveryAddress")]
        public SagaDeliveryAddress DeliveryAddress { get; set; }

        [JsonPropertyName("paymentMethod")]
        public string PaymentMethod { get; set; } = "CREDIT_CARD";

        [JsonPropertyName("orderType")]
        public string OrderType { get; set; } = "DELIVERY";

        [JsonPropertyName("notes")]
        public string Notes { get; set; }

        /// <summary>
        /// iyzico callback URL — ödeme formu tamamlandıktan sonra yönlendirilecek adres.
        /// {paymentId} placeholder'ı otomatik doldurulur.
        /// </summary>
        [JsonPropertyName("callbackUrl")]
        public string CallbackUrl { get; set; }
    }

    public class SagaDeliveryAddress
    {
        [JsonPropertyName("street")]
        public string Street { get; set; }

        [JsonPropertyName("district")]
        public string District { get; set; }

        [JsonPropertyName("city")]
        public string City { get; set; }

        [JsonPropertyName("postalCode")]
        public string PostalCode { get; set; }

        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lng")]
        public double Lng { get; set; }
    }
}
