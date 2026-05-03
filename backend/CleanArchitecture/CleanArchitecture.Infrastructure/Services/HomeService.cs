using CleanArchitecture.Core.DTOs.Home;
using CleanArchitecture.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Services
{
    public class HomeService : IHomeService
    {
        private readonly IVendorService _vendorService;

        public HomeService(IVendorService vendorService)
        {
            _vendorService = vendorService;
        }

        public async Task<DiscoverResponse> GetDiscoverAsync(string userId)
        {
            var vendorsResponse = await _vendorService.GetVendorsAsync(1, 10);
            
            var featuredVendors = vendorsResponse.Data.Select(v => new FeaturedVendorDto
            {
                Id = v.Id,
                Name = v.Name,
                Rating = v.Rating,
                ReviewCount = v.ReviewCount,
                Eta = v.DeliveryInfo?.EtaRange ?? "20-30 dk",
                DeliveryFee = v.DeliveryInfo?.DeliveryFee ?? 0,
                ImageUrl = v.LogoUrl
            }).ToList();

            var response = new DiscoverResponse
            {
                ActiveOrder = null,
                HeroBanners = new List<HeroBannerDto>(),
                PrimaryCategories = new List<CategoryDto>(),
                MiniServices = new List<MiniServiceDto>(),
                FeaturedVendors = featuredVendors
            };

            return response;
        }
    }
}
