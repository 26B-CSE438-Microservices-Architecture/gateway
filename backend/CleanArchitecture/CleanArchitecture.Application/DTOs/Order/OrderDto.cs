using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CleanArchitecture.Core.DTOs.Order
{
    // --- Cart Models ---
    public class CartResponse
    {
        [JsonPropertyName("items")]
        public List<CartItemDto> Items { get; set; } = new();

        [JsonPropertyName("totalAmount")]
        public double TotalAmount { get; set; }
    }

    public class CartItemDto
    {
        [JsonPropertyName("productId")]
        public string ProductId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("price")]
        public double Price { get; set; }

        [JsonPropertyName("subtotal")]
        public double Subtotal => Quantity * Price;
    }

    public class AddCartItemRequest
    {
        [JsonPropertyName("productId")]
        public string ProductId { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }
    }

    public class UpdateCartItemRequest
    {
        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }
    }

    // --- Order Models ---
    public class CheckoutRequest
    {
        [JsonPropertyName("deliveryAddress")]
        public OrderAddressDto DeliveryAddress { get; set; }

        [JsonPropertyName("paymentMethod")]
        public string PaymentMethod { get; set; } = "CREDIT_CARD";

        [JsonPropertyName("orderType")]
        public string OrderType { get; set; } = "DELIVERY";

        [JsonPropertyName("notes")]
        public string Notes { get; set; }
    }

    public class OrderAddressDto
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

    public class OrderResponse
    {
        [JsonPropertyName("orderId")]
        public string OrderId { get; set; }

        [JsonPropertyName("userId")]
        public string UserId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("totalAmount")]
        public double TotalAmount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "TRY";

        [JsonPropertyName("items")]
        public List<OrderItemDto> Items { get; set; }

        [JsonPropertyName("deliveryAddress")]
        public OrderAddressDto DeliveryAddress { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }

    public class OrderItemDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("price")]
        public double Price { get; set; }
    }

    public class InternalPaymentCallbackRequest
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } // HOLD_CONFIRMED, CAPTURE_COMPLETED, etc.
    }

    public class UpdateOrderStatusRequest
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }
    }
    
    public class PagedResponse<T>
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("size")]
        public int Size { get; set; }

        [JsonPropertyName("total")]
        public long Total { get; set; }

        [JsonPropertyName("data")]
        public List<T> Data { get; set; }
    }
}
