using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CleanArchitecture.Core.DTOs.Vendor
{
    public class VendorLookupRequest
    {
        [JsonPropertyName("vendorIds")]
        public List<string> VendorIds { get; set; }
    }

    public class VendorLookupResponse
    {
        [JsonPropertyName("vendors")]
        public List<VendorLookupItemDto> Vendors { get; set; }
    }

    public class VendorLookupItemDto
    {
        [JsonPropertyName("vendor_id")]
        public string VendorId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("image_url")]
        public string ImageUrl { get; set; }
    }
}
