using CleanArchitecture.Core.DTOs.Home;
using CleanArchitecture.Core.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Services
{
    public class HomeService : IHomeService
    {
        public Task<DiscoverResponse> GetDiscoverAsync(string userId)
        {
            var response = new DiscoverResponse
            {
                ActiveOrder = new ActiveOrderDto
                {
                    Id = "order_555",
                    VendorName = "Burger Point",
                    Status = "DELIVERING",
                    StatusLabel = "Sipariþin Hazýrlanýyor"
                },
                HeroBanners = new List<HeroBannerDto>
                {
                    new HeroBannerDto
                    {
                        Id = "banner_1",
                        Title = "300 TL üzeri",
                        Subtitle = "40 TL indirim",
                        ImageUrl = "https://cdn.app.com/banner1.png"
                    }
                },
                PrimaryCategories = new List<CategoryDto>
                {
                    new CategoryDto { Id = "food", Name = "Yemek", IconUrl = "https://cdn.app.com/icons/food.png" },
                    new CategoryDto { Id = "grocery", Name = "Market", IconUrl = "https://cdn.app.com/icons/grocery.png" },
                    new CategoryDto { Id = "water", Name = "Su & Ýçecek", IconUrl = "https://cdn.app.com/icons/water.png" }
                },
                MiniServices = new List<MiniServiceDto>
                {
                    new MiniServiceDto { Id = "petshop", Name = "Petshop", IconUrl = "https://cdn.app.com/icons/pet.png" },
                    new MiniServiceDto { Id = "flowers", Name = "Çiçek", IconUrl = "https://cdn.app.com/icons/flowers.png" }
                },
                FeaturedVendors = new List<FeaturedVendorDto>
                {
                    new FeaturedVendorDto
                    {
                        Id = "vendor_101",
                        Name = "Burger Point",
                        Rating = 4.7,
                        ReviewCount = 1280,
                        Eta = "20-30 dk",
                        DeliveryFee = 24.9,
                        ImageUrl = "https://cdn.app.com/burgerpoint.jpg"
                    },
                    new FeaturedVendorDto
                    {
                        Id = "vendor_102",
                        Name = "Pizza Express",
                        Rating = 4.5,
                        ReviewCount = 980,
                        Eta = "25-35 dk",
                        DeliveryFee = 19.9,
                        ImageUrl = "https://cdn.app.com/pizzaexpress.jpg"
                    }
                }
            };

            return Task.FromResult(response);
        }
    }
}
