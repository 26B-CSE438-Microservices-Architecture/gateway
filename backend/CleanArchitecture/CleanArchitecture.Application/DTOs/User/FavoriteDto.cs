using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CleanArchitecture.Core.DTOs.User
{
    public class FavoriteVendorDto
    {
        [JsonPropertyName("vendor_id")]
        public string VendorId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("image_url")]
        public string ImageUrl { get; set; }
    }

    public class PagedFavoritesResponse
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("data")]
        public List<FavoriteVendorDto> Data { get; set; }
    }
}
