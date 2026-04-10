using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CleanArchitecture.Core.DTOs.Order
{
    public class CheckoutPreviewRequest
    {
        [JsonPropertyName("vendor_id")]
        public string VendorId { get; set; }

        [JsonPropertyName("items")]
        public List<OrderItemRequest> Items { get; set; }
    }

    public class OrderItemRequest
    {
        [JsonPropertyName("product_id")]
        public string ProductId { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }
    }

    public class CheckoutPreviewResponse
    {
        [JsonPropertyName("items_subtotal")]
        public double ItemsSubtotal { get; set; }

        [JsonPropertyName("delivery_fee")]
        public double DeliveryFee { get; set; }

        [JsonPropertyName("service_fee")]
        public double ServiceFee { get; set; }

        [JsonPropertyName("discount_amount")]
        public double DiscountAmount { get; set; }

        [JsonPropertyName("total_amount")]
        public double TotalAmount { get; set; }
    }

    public class CreateOrderRequest
    {
        [JsonPropertyName("vendor_id")]
        public string VendorId { get; set; }

        [JsonPropertyName("address_id")]
        public string AddressId { get; set; }

        [JsonPropertyName("items")]
        public List<OrderItemRequest> Items { get; set; }

        [JsonPropertyName("payment_method_id")]
        public string PaymentMethodId { get; set; }

        [JsonPropertyName("note")]
        public string Note { get; set; }
    }

    public class CreateOrderResponse
    {
        [JsonPropertyName("order_id")]
        public string OrderId { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("vendor_id")]
        public string VendorId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("total_price")]
        public double TotalPrice { get; set; }

        [JsonPropertyName("address_snapshot")]
        public AddressSnapshotDto AddressSnapshot { get; set; }
    }

    public class AddressSnapshotDto
    {
        [JsonPropertyName("address_title")]
        public string AddressTitle { get; set; }

        [JsonPropertyName("city")]
        public string City { get; set; }

        [JsonPropertyName("district")]
        public string District { get; set; }

        [JsonPropertyName("neighborhood")]
        public string Neighborhood { get; set; }

        [JsonPropertyName("street")]
        public string Street { get; set; }

        [JsonPropertyName("building_no")]
        public string BuildingNo { get; set; }

        [JsonPropertyName("floor")]
        public string Floor { get; set; }

        [JsonPropertyName("apartment_no")]
        public string ApartmentNo { get; set; }

        [JsonPropertyName("address_description")]
        public string AddressDescription { get; set; }

        [JsonPropertyName("location")]
        public OrderLocationDto Location { get; set; }
    }

    public class OrderLocationDto
    {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lng")]
        public double Lng { get; set; }
    }

    public class OrderSummaryDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("vendor_name")]
        public string VendorName { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("total_amount")]
        public double TotalAmount { get; set; }

        [JsonPropertyName("date_label")]
        public string DateLabel { get; set; }

        [JsonPropertyName("address_snapshot")]
        public AddressSnapshotDto AddressSnapshot { get; set; }

        [JsonPropertyName("item_summary")]
        public string ItemSummary { get; set; }

        [JsonPropertyName("delivered_item_count")]
        public int DeliveredItemCount { get; set; }
    }

    public class OrderDetailDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("status_label")]
        public string StatusLabel { get; set; }

        [JsonPropertyName("eta_range")]
        public string EtaRange { get; set; }

        [JsonPropertyName("active_step_index")]
        public int ActiveStepIndex { get; set; }

        [JsonPropertyName("address_snapshot")]
        public AddressSnapshotDto AddressSnapshot { get; set; }

        [JsonPropertyName("steps")]
        public List<OrderStepDto> Steps { get; set; }
    }

    public class OrderStepDto
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("is_completed")]
        public bool IsCompleted { get; set; }
    }

    public class PagedOrdersResponse
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("data")]
        public List<OrderSummaryDto> Data { get; set; }
    }
}
