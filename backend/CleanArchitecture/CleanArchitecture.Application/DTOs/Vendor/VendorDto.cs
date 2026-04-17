using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CleanArchitecture.Core.DTOs.Vendor
{
    public class VendorSummaryDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("kind")]
        public string Kind { get; set; }

        [JsonPropertyName("rating")]
        public double Rating { get; set; }

        [JsonPropertyName("review_count")]
        public int ReviewCount { get; set; }

        [JsonPropertyName("distance_km")]
        public double DistanceKm { get; set; }

        [JsonPropertyName("campaign_badges")]
        public List<string> CampaignBadges { get; set; }

        [JsonPropertyName("working_hours")]
        public WorkingHoursDto WorkingHours { get; set; }

        [JsonPropertyName("delivery_info")]
        public DeliveryInfoDto DeliveryInfo { get; set; }
    }

    public class VendorDetailDto : VendorSummaryDto
    {
        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("address_text")]
        public string AddressText { get; set; }

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("logo_url")]
        public string LogoUrl { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } // Open, Closed, Busy

        [JsonPropertyName("menu_sections")]
        public List<MenuSectionDto> MenuSections { get; set; }
    }

    public class CreateVendorDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("address_text")]
        public string AddressText { get; set; }

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("logo_url")]
        public string LogoUrl { get; set; }

        [JsonPropertyName("min_order_amount")]
        public double MinOrderAmount { get; set; }

        [JsonPropertyName("delivery_fee")]
        public double DeliveryFee { get; set; }

        [JsonPropertyName("opening_time")]
        public string OpeningTime { get; set; }

        [JsonPropertyName("closing_time")]
        public string ClosingTime { get; set; }
    }

    public class UpdateVendorDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("address_text")]
        public string AddressText { get; set; }

        [JsonPropertyName("latitude")]
        public double? Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double? Longitude { get; set; }

        [JsonPropertyName("logo_url")]
        public string LogoUrl { get; set; }

        [JsonPropertyName("min_order_amount")]
        public double? MinOrderAmount { get; set; }

        [JsonPropertyName("delivery_fee")]
        public double? DeliveryFee { get; set; }
    }

    public class UpdateStatusDto
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }
    }

    public class CreateCategoryDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("display_order")]
        public int DisplayOrder { get; set; }
    }

    public class UpdateCategoryDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("display_order")]
        public int? DisplayOrder { get; set; }
    }

    public class CreateProductDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("price")]
        public double Price { get; set; }

        [JsonPropertyName("image_url")]
        public string ImageUrl { get; set; }
    }

    public class UpdateProductDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("price")]
        public double? Price { get; set; }

        [JsonPropertyName("image_url")]
        public string ImageUrl { get; set; }
    }

    public class UpdateStockDto
    {
        [JsonPropertyName("is_available")]
        public bool IsAvailable { get; set; }
    }

    public class WorkingHoursDto
    {
        [JsonPropertyName("open")]
        public string Open { get; set; }

        [JsonPropertyName("close")]
        public string Close { get; set; }

        [JsonPropertyName("is_open")]
        public bool IsOpen { get; set; }
    }

    public class DeliveryInfoDto
    {
        [JsonPropertyName("eta_range")]
        public string EtaRange { get; set; }

        [JsonPropertyName("minimum_basket_amount")]
        public double MinimumBasketAmount { get; set; }

        [JsonPropertyName("delivery_fee")]
        public double DeliveryFee { get; set; }
    }

    public class MenuSectionDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("products")]
        public List<ProductDto> Products { get; set; }
    }

    public class ProductDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("price")]
        public double Price { get; set; }

        [JsonPropertyName("badge")]
        public string Badge { get; set; }

        [JsonPropertyName("image_url")]
        public string ImageUrl { get; set; }

        [JsonPropertyName("is_available")]
        public bool IsAvailable { get; set; }

        [JsonPropertyName("allergens")]
        public List<string> Allergens { get; set; }

        [JsonPropertyName("calories")]
        public int Calories { get; set; }

        [JsonPropertyName("option_groups")]
        public List<OptionGroupDto> OptionGroups { get; set; }
    }

    public class OptionGroupDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("is_required")]
        public bool IsRequired { get; set; }

        [JsonPropertyName("max_selections")]
        public int MaxSelections { get; set; }

        [JsonPropertyName("options")]
        public List<OptionItemDto> Options { get; set; }
    }

    public class OptionItemDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("price")]
        public double Price { get; set; }
    }

    public class PagedVendorsResponse
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("data")]
        public List<VendorSummaryDto> Data { get; set; }
    }
}
