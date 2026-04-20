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
                LogoUrl = "https://cdn.app.com/burger_logo.png",
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
                                ImageUrl = "https://cdn.app.com/burger.png",
                                IsAvailable = true
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
                LogoUrl = "https://cdn.app.com/pizza_logo.png",
                CampaignBadges = new List<string> { "İlk siparişe %20 indirim" },
                WorkingHours = new WorkingHoursDto { Open = "11:00", Close = "23:30", IsOpen = true },
                DeliveryInfo = new DeliveryInfoDto { EtaRange = "25-35 dk", MinimumBasketAmount = 150, DeliveryFee = 19.9 },
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

        public Task<PagedVendorsResponse> GetNearbyVendorsAsync(double lat, double lng, double radiusKm)
        {
            var data = _vendors.Select(v => (VendorSummaryDto)v).ToList();
            return Task.FromResult(new PagedVendorsResponse
            {
                Page = 1,
                Limit = data.Count,
                Total = data.Count,
                Data = data
            });
        }

        public Task<List<VendorLookupItemDto>> LookupVendorsAsync(List<string> vendorIds)
        {
            return Task.FromResult(_vendors
                .Where(v => vendorIds.Contains(v.Id))
                .Select(v => new VendorLookupItemDto
                {
                    VendorId = v.Id,
                    Name = v.Name,
                    ImageUrl = v.LogoUrl
                })
                .ToList());
        }

        public Task<string> CreateVendorAsync(CreateVendorDto request)
        {
            var id = $"vendor_{System.Guid.NewGuid():N}";
            var vendor = new VendorDetailDto
            {
                Id = id,
                Name = request.Name,
                Description = request.Description,
                AddressText = request.AddressText,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                LogoUrl = request.LogoUrl,
                Status = "Open",
                DeliveryInfo = new DeliveryInfoDto { MinimumBasketAmount = request.MinOrderAmount, DeliveryFee = request.DeliveryFee, EtaRange = "20-30 dk" },
                WorkingHours = new WorkingHoursDto { Open = request.OpeningTime, Close = request.ClosingTime, IsOpen = true },
                MenuSections = new List<MenuSectionDto>()
            };
            _vendors.Add(vendor);
            return Task.FromResult(id);
        }

        public Task<bool> UpdateVendorAsync(string vendorId, UpdateVendorDto request)
        {
            var vendor = _vendors.FirstOrDefault(v => v.Id == vendorId);
            if (vendor == null) return Task.FromResult(false);

            vendor.Name = request.Name ?? vendor.Name;
            vendor.Description = request.Description ?? vendor.Description;
            vendor.AddressText = request.AddressText ?? vendor.AddressText;
            vendor.Latitude = request.Latitude ?? vendor.Latitude;
            vendor.Longitude = request.Longitude ?? vendor.Longitude;
            vendor.LogoUrl = request.LogoUrl ?? vendor.LogoUrl;
            
            if (request.MinOrderAmount.HasValue) vendor.DeliveryInfo.MinimumBasketAmount = request.MinOrderAmount.Value;
            if (request.DeliveryFee.HasValue) vendor.DeliveryInfo.DeliveryFee = request.DeliveryFee.Value;

            return Task.FromResult(true);
        }

        public Task<bool> UpdateVendorStatusAsync(string vendorId, UpdateStatusDto request)
        {
            var vendor = _vendors.FirstOrDefault(v => v.Id == vendorId);
            if (vendor == null) return Task.FromResult(false);

            vendor.Status = request.Status;
            vendor.WorkingHours.IsOpen = request.Status == "Open";

            return Task.FromResult(true);
        }

        public Task<bool> DeleteVendorAsync(string vendorId)
        {
            var vendor = _vendors.FirstOrDefault(v => v.Id == vendorId);
            if (vendor == null) return Task.FromResult(false);
            _vendors.Remove(vendor);
            return Task.FromResult(true);
        }

        public Task<string> CreateCategoryAsync(string vendorId, CreateCategoryDto request)
        {
            var vendor = _vendors.FirstOrDefault(v => v.Id == vendorId);
            if (vendor == null) throw new NotFoundException("VENDOR_NOT_FOUND", "Vendor not found");

            var id = $"cat_{System.Guid.NewGuid():N}";
            vendor.MenuSections.Add(new MenuSectionDto
            {
                Id = id,
                Title = request.Name,
                Products = new List<ProductDto>()
            });

            return Task.FromResult(id);
        }

        public Task<bool> UpdateCategoryAsync(string categoryId, UpdateCategoryDto request)
        {
            var category = _vendors.SelectMany(v => v.MenuSections).FirstOrDefault(c => c.Id == categoryId);
            if (category == null) return Task.FromResult(false);

            category.Title = request.Name ?? category.Title;
            return Task.FromResult(true);
        }

        public Task<bool> DeleteCategoryAsync(string categoryId)
        {
            foreach (var vendor in _vendors)
            {
                var category = vendor.MenuSections.FirstOrDefault(c => c.Id == categoryId);
                if (category != null)
                {
                    vendor.MenuSections.Remove(category);
                    return Task.FromResult(true);
                }
            }
            return Task.FromResult(false);
        }

        public Task<string> CreateProductAsync(string categoryId, CreateProductDto request)
        {
            var category = _vendors.SelectMany(v => v.MenuSections).FirstOrDefault(c => c.Id == categoryId);
            if (category == null) throw new NotFoundException("CATEGORY_NOT_FOUND", "Category not found");

            var id = $"prod_{System.Guid.NewGuid():N}";
            category.Products.Add(new ProductDto
            {
                Id = id,
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                ImageUrl = request.ImageUrl,
                IsAvailable = true
            });

            return Task.FromResult(id);
        }

        public Task<bool> UpdateProductAsync(string productId, UpdateProductDto request)
        {
            var product = _vendors.SelectMany(v => v.MenuSections).SelectMany(c => c.Products).FirstOrDefault(p => p.Id == productId);
            if (product == null) return Task.FromResult(false);

            product.Name = request.Name ?? product.Name;
            product.Description = request.Description ?? product.Description;
            if (request.Price.HasValue) product.Price = request.Price.Value;
            product.ImageUrl = request.ImageUrl ?? product.ImageUrl;

            return Task.FromResult(true);
        }

        public Task<bool> UpdateProductStockAsync(string productId, UpdateStockDto request)
        {
            var product = _vendors.SelectMany(v => v.MenuSections).SelectMany(c => c.Products).FirstOrDefault(p => p.Id == productId);
            if (product == null) return Task.FromResult(false);

            product.IsAvailable = request.IsAvailable;
            return Task.FromResult(true);
        }

        public Task<bool> DeleteProductAsync(string productId)
        {
            foreach (var vendor in _vendors)
            {
                foreach (var category in vendor.MenuSections)
                {
                    var product = category.Products.FirstOrDefault(p => p.Id == productId);
                    if (product != null)
                    {
                        category.Products.Remove(product);
                        return Task.FromResult(true);
                    }
                }
            }
            return Task.FromResult(false);
        }
    }
}
