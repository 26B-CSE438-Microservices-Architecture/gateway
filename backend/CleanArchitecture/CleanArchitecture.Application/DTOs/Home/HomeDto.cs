using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CleanArchitecture.Core.DTOs.Home
{
    public class DiscoverResponse
    {
        [JsonPropertyName("active_order")]
        public ActiveOrderDto ActiveOrder { get; set; }

        [JsonPropertyName("hero_banners")]
        public List<HeroBannerDto> HeroBanners { get; set; }

        [JsonPropertyName("primary_categories")]
        public List<CategoryDto> PrimaryCategories { get; set; }

        [JsonPropertyName("mini_services")]
        public List<MiniServiceDto> MiniServices { get; set; }

        [JsonPropertyName("featured_vendors")]
        public List<FeaturedVendorDto> FeaturedVendors { get; set; }
    }

    public class ActiveOrderDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("vendor_name")]
        public string VendorName { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("status_label")]
        public string StatusLabel { get; set; }
    }

    public class HeroBannerDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("subtitle")]
        public string Subtitle { get; set; }

        [JsonPropertyName("image_url")]
        public string ImageUrl { get; set; }
    }

    public class CategoryDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("icon_url")]
        public string IconUrl { get; set; }
    }

    public class MiniServiceDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("icon_url")]
        public string IconUrl { get; set; }
    }

    public class FeaturedVendorDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("rating")]
        public double Rating { get; set; }

        [JsonPropertyName("review_count")]
        public int ReviewCount { get; set; }

        [JsonPropertyName("eta")]
        public string Eta { get; set; }

        [JsonPropertyName("delivery_fee")]
        public double DeliveryFee { get; set; }

        [JsonPropertyName("image_url")]
        public string ImageUrl { get; set; }
    }
}
