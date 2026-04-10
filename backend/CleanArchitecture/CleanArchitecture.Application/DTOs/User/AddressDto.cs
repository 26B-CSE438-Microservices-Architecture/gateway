using System.Text.Json.Serialization;

namespace CleanArchitecture.Core.DTOs.User
{
    public class AddressDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

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

        [JsonPropertyName("phone")]
        public string Phone { get; set; }

        [JsonPropertyName("location")]
        public LocationDto Location { get; set; }

        [JsonPropertyName("masked_phone")]
        public string MaskedPhone { get; set; }

        [JsonPropertyName("shows_map_preview")]
        public bool ShowsMapPreview { get; set; }

        [JsonPropertyName("is_current")]
        public bool IsCurrent { get; set; }
    }

    public class LocationDto
    {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lng")]
        public double Lng { get; set; }
    }

    public class CreateAddressRequest
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

        [JsonPropertyName("phone")]
        public string Phone { get; set; }

        [JsonPropertyName("location")]
        public LocationDto Location { get; set; }
    }

    public class UpdateAddressRequest
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

        [JsonPropertyName("phone")]
        public string Phone { get; set; }

        [JsonPropertyName("location")]
        public LocationDto Location { get; set; }
    }
}
