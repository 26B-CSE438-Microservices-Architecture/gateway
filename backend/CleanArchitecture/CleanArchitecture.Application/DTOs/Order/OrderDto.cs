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

        /// <summary>
        /// Opsiyonel: Frontend'in bildiği restaurantId.
        /// Gönderilirse VendorService'e çağrı yapılmaz, direkt kullanılır.
        /// </summary>
        [JsonPropertyName("restaurantId")]
        public string RestaurantId { get; set; }

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

    public class SyncProductRequest
    {
        [JsonPropertyName("productId")]
        public string ProductId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("price")]
        public double Price { get; set; }

        [JsonPropertyName("vendorId")]
        public string VendorId { get; set; }
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

    // --- Order Service Internal Request (Gateway → Java) ---
    public class OrderServiceAddCartItemRequest
    {
        [JsonPropertyName("menuItemId")]
        public string MenuItemId { get; set; }

        [JsonPropertyName("restaurantId")]
        public string RestaurantId { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("specialInstructions")]
        public string SpecialInstructions { get; set; }
    }

    // --- Order Service Internal Responses (Java → Gateway) ---
    public class OsCartResponse
    {
        [JsonPropertyName("cartId")]
        public string CartId { get; set; }

        [JsonPropertyName("restaurantId")]
        public string RestaurantId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("items")]
        public List<OsCartItem> Items { get; set; } = new();

        [JsonPropertyName("total")]
        public OsMoney Total { get; set; }
    }

    public class OsCartItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("menuItemId")]
        public string MenuItemId { get; set; }

        [JsonPropertyName("menuItemName")]
        public string MenuItemName { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("unitPrice")]
        public OsMoney UnitPrice { get; set; }

        [JsonPropertyName("totalPrice")]
        public OsMoney TotalPrice { get; set; }

        [JsonPropertyName("specialInstructions")]
        public string SpecialInstructions { get; set; }
    }

    public class OsMoney
    {
        [JsonPropertyName("amount")]
        public double Amount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }
    }
}
