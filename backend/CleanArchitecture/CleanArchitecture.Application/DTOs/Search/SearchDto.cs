using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CleanArchitecture.Core.DTOs.Search
{
    public class DiscoveryResponse
    {
        [JsonPropertyName("sections")]
        public List<DiscoverySectionDto> Sections { get; set; }
    }

    public class DiscoverySectionDto
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("items")]
        public List<DiscoveryItemDto> Items { get; set; }
    }

    public class DiscoveryItemDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("logo_url")]
        public string LogoUrl { get; set; }

        [JsonPropertyName("image_url")]
        public string ImageUrl { get; set; }

        [JsonPropertyName("color_code")]
        public string ColorCode { get; set; }
    }

    public class SearchResponse
    {
        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        [JsonPropertyName("vendors")]
        public List<SearchVendorDto> Vendors { get; set; }

        [JsonPropertyName("products")]
        public List<SearchProductDto> Products { get; set; }
    }

    public class SearchVendorDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("rating")]
        public double Rating { get; set; }

        [JsonPropertyName("is_sponsored")]
        public bool IsSponsored { get; set; }

        [JsonPropertyName("image_url")]
        public string ImageUrl { get; set; }

        [JsonPropertyName("eta")]
        public string Eta { get; set; }
    }

    public class SearchProductDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("price_label")]
        public string PriceLabel { get; set; }

        [JsonPropertyName("vendor_name")]
        public string VendorName { get; set; }
    }
}
