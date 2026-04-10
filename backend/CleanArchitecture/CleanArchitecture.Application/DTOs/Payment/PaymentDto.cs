using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CleanArchitecture.Core.DTOs.Payment
{
    public class PaymentMethodsResponse
    {
        [JsonPropertyName("wallet_balance")]
        public double WalletBalance { get; set; }

        [JsonPropertyName("saved_cards")]
        public List<SavedCardDto> SavedCards { get; set; }
    }

    public class SavedCardDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("detail")]
        public string Detail { get; set; }

        [JsonPropertyName("is_default")]
        public bool IsDefault { get; set; }
    }

    public class AddCardRequest
    {
        [JsonPropertyName("card_number")]
        public string CardNumber { get; set; }

        [JsonPropertyName("expire_month")]
        public string ExpireMonth { get; set; }

        [JsonPropertyName("expire_year")]
        public string ExpireYear { get; set; }

        [JsonPropertyName("cvv")]
        public string Cvv { get; set; }
    }

    public class AddCardResponse
    {
        [JsonPropertyName("card_id")]
        public string CardId { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }

    public class PaymentIntentRequest
    {
        [JsonPropertyName("order_id")]
        public string OrderId { get; set; }

        [JsonPropertyName("payment_method_id")]
        public string PaymentMethodId { get; set; }
    }

    public class PaymentIntentResponse
    {
        [JsonPropertyName("client_secret")]
        public string ClientSecret { get; set; }
    }
}
