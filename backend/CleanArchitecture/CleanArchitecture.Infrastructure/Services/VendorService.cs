using CleanArchitecture.Core.DTOs.Vendor;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Services
{
    public class VendorService : IVendorService
    {
        private static readonly List<VendorDetailDto> _vendors = new List<VendorDetailDto>
        {
            new VendorDetailDto
            {
                Id = "vendor_101",
                Name = "Burger Point",
                Kind = "RESTAURANT",
                Rating = 4.7,
                ReviewCount = 1280,
                DistanceKm = 2.4,
                CampaignBadges = new List<string> { "30 TL indirim", "Ücretsiz teslimat" },
                WorkingHours = new WorkingHoursDto { Open = "10:00", Close = "02:00", IsOpen = true },
                DeliveryInfo = new DeliveryInfoDto { EtaRange = "20-30 dk", MinimumBasketAmount = 180, DeliveryFee = 24.9 },
                MenuSections = new List<MenuSectionDto>
                {
                    new MenuSectionDto
                    {
                        Id = "section_1",
                        Title = "Burger Menüler",
                        Products = new List<ProductDto>
                        {
                            new ProductDto
                            {
                                Id = "prod_1",
                                Name = "Double Smash Burger",
                                Description = "Çift köfte cheddar peynir",
                                Price = 210,
                                Badge = "En Çok Satan",
                                ImageUrl = "https://cdn.app.com/burger.png",
                                IsAvailable = true,
                                Allergens = new List<string> { "gluten", "dairy" },
                                Calories = 820,
                                OptionGroups = new List<OptionGroupDto>
                                {
                                    new OptionGroupDto
                                    {
                                        Id = "drink",
                                        Title = "Ýçecek Seçimi",
                                        IsRequired = true,
                                        MaxSelections = 1,
                                        Options = new List<OptionItemDto>
                                        {
                                            new OptionItemDto { Name = "Kola", Price = 0 },
                                            new OptionItemDto { Name = "Ayran", Price = 5 }
                                        }
                                    }
                                }
                            },
                            new ProductDto
                            {
                                Id = "prod_2",
                                Name = "Classic Burger",
                                Description = "Klasik burger, marul, domates, özel sos",
                                Price = 160,
                                Badge = null,
                                ImageUrl = "https://cdn.app.com/classic.png",
                                IsAvailable = true,
                                Allergens = new List<string> { "gluten" },
                                Calories = 650,
                                OptionGroups = new List<OptionGroupDto>()
                            }
                        }
                    },
                    new MenuSectionDto
                    {
                        Id = "section_2",
                        Title = "Yanlar",
                        Products = new List<ProductDto>
                        {
                            new ProductDto
                            {
                                Id = "prod_3",
                                Name = "Patates Kýzartmasý",
                                Description = "Çýtýr patates",
                                Price = 60,
                                Badge = null,
                                ImageUrl = "https://cdn.app.com/fries.png",
                                IsAvailable = true,
                                Allergens = new List<string>(),
                                Calories = 380,
                                OptionGroups = new List<OptionGroupDto>()
                            }
                        }
                    }
                }
            },
            new VendorDetailDto
            {
                Id = "vendor_102",
                Name = "Pizza Express",
                Kind = "RESTAURANT",
                Rating = 4.5,
                ReviewCount = 980,
                DistanceKm = 1.8,
                CampaignBadges = new List<string> { "Ýlk sipariþe %20 indirim" },
                WorkingHours = new WorkingHoursDto { Open = "11:00", Close = "23:30", IsOpen = true },
                DeliveryInfo = new DeliveryInfoDto { EtaRange = "25-35 dk", MinimumBasketAmount = 150, DeliveryFee = 19.9 },
                MenuSections = new List<MenuSectionDto>
                {
                    new MenuSectionDto
                    {
                        Id = "section_3",
                        Title = "Pizzalar",
                        Products = new List<ProductDto>
                        {
                            new ProductDto
                            {
                                Id = "prod_4",
                                Name = "Margherita",
                                Description = "Domates sos, mozzarella",
                                Price = 180,
                                Badge = null,
                                ImageUrl = "https://cdn.app.com/margherita.png",
                                IsAvailable = true,
                                Allergens = new List<string> { "gluten", "dairy" },
                                Calories = 720,
                                OptionGroups = new List<OptionGroupDto>()
                            }
                        }
                    }
                }
            },
            new VendorDetailDto
            {
                Id = "vendor_103",
                Name = "Komagene Çið Köfte",
                Kind = "RESTAURANT",
                Rating = 4.3,
                ReviewCount = 560,
                DistanceKm = 0.9,
                CampaignBadges = new List<string>(),
                WorkingHours = new WorkingHoursDto { Open = "09:00", Close = "00:00", IsOpen = true },
                DeliveryInfo = new DeliveryInfoDto { EtaRange = "15-25 dk", MinimumBasketAmount = 80, DeliveryFee = 0 },
                MenuSections = new List<MenuSectionDto>()
            }
        };

        public Task<PagedVendorsResponse> GetVendorsAsync(int page, int limit)
        {
            var paged = _vendors.Skip((page - 1) * limit).Take(limit)
                .Select(v => (VendorSummaryDto)v)
                .ToList();

            return Task.FromResult(new PagedVendorsResponse
            {
                Page = page,
                Limit = limit,
                Total = _vendors.Count,
                Data = paged
            });
        }

        public Task<VendorDetailDto> GetVendorByIdAsync(string vendorId)
        {
            var vendor = _vendors.FirstOrDefault(v => v.Id == vendorId);
            if (vendor == null)
                throw new NotFoundException("VENDOR_NOT_FOUND", "Vendor not found");

            return Task.FromResult(vendor);
        }
    }
}
