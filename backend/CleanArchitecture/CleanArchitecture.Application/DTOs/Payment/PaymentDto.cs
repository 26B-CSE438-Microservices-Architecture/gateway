using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CleanArchitecture.Core.DTOs.Payment
{
    public class PaymentInitRequest
    {
        [JsonPropertyName("orderId")]
        public string OrderId { get; set; }

        [JsonPropertyName("amount")]
        public int Amount { get; set; } // Minor units (kuruş)

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "TRY";

        [JsonPropertyName("paymentMethod")]
        public string PaymentMethod { get; set; } = "card";

        [JsonPropertyName("buyer")]
        public BuyerDto Buyer { get; set; }

        [JsonPropertyName("items")]
        public List<PaymentItemDto> Items { get; set; }

        [JsonPropertyName("callbackUrl")]
        public string CallbackUrl { get; set; }
    }

    public class BuyerDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string IdentityNumber { get; set; }
        public string GsmNumber { get; set; }
        public string RegistrationAddress { get; set; }
        public string Ip { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string ZipCode { get; set; }
    }

    public class PaymentItemDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Category1 { get; set; }
        public string ItemType { get; set; } = "PHYSICAL";
        public string Price { get; set; } // Major units as string, e.g. "150.00"
    }

    public class PaymentResponse
    {
        public string Id { get; set; }
        public string OrderId { get; set; }
        public string UserId { get; set; }
        public string Status { get; set; }
        public int Amount { get; set; }
        public string Currency { get; set; }
        public string Provider { get; set; } = "iyzico";
        public string ProviderTxId { get; set; }
        public string FailureReason { get; set; }
        public string CancelReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? AuthorizedAt { get; set; }
        public DateTime? CapturedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
    }

    public class CheckoutFormDetails
    {
        public string Token { get; set; }
        public string Content { get; set; } // Base64 HTML
        public string PaymentPageUrl { get; set; }
    }

    public class PaymentInitResponse
    {
        [JsonPropertyName("payment")]
        public PaymentResponse Payment { get; set; }

        [JsonPropertyName("checkoutForm")]
        public CheckoutFormDetails CheckoutForm { get; set; }
    }

    public class PaymentCallbackRequest
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }
    }

    public class PaymentCaptureRequest
    {
        [JsonPropertyName("amount")]
        public int Amount { get; set; }
    }

    public class PaymentCancelRequest
    {
        [JsonPropertyName("reason")]
        public string Reason { get; set; }
    }
}
