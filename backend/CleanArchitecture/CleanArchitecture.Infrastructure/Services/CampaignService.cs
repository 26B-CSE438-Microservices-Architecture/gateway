using CleanArchitecture.Core.DTOs.Campaign;
using CleanArchitecture.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Services
{
    public class CampaignService : ICampaignService
    {
        private static readonly List<CampaignDto> _campaigns = new List<CampaignDto>
        {
            new CampaignDto { Id = "camp_1", Title = "300 TL üzeri 40 TL indirim", MinimumBasket = 300, DiscountAmount = 40 },
            new CampaignDto { Id = "camp_2", Title = "Ýlk sipariþe %20 indirim", MinimumBasket = 0, DiscountAmount = 0 },
            new CampaignDto { Id = "camp_3", Title = "200 TL üzeri ücretsiz teslimat", MinimumBasket = 200, DiscountAmount = 0 },
            new CampaignDto { Id = "camp_4", Title = "Hafta sonu %15 indirim", MinimumBasket = 100, DiscountAmount = 0 },
            new CampaignDto { Id = "camp_5", Title = "Yeni kullanýcý 50 TL indirim", MinimumBasket = 150, DiscountAmount = 50 }
        };

        private static readonly Dictionary<string, List<VendorCampaignDto>> _vendorCampaigns = new Dictionary<string, List<VendorCampaignDto>>
        {
            ["vendor_101"] = new List<VendorCampaignDto>
            {
                new VendorCampaignDto { Id = "camp_1", Title = "300 TL üzeri 40 TL indirim" },
                new VendorCampaignDto { Id = "camp_3", Title = "200 TL üzeri ücretsiz teslimat" },
                new VendorCampaignDto { Id = "camp_4", Title = "Hafta sonu %15 indirim" }
            },
            ["vendor_102"] = new List<VendorCampaignDto>
            {
                new VendorCampaignDto { Id = "camp_2", Title = "Ýlk sipariþe %20 indirim" }
            }
        };

        public Task<PagedCampaignsResponse<CampaignDto>> GetCampaignsAsync(int page, int limit)
        {
            var paged = _campaigns.Skip((page - 1) * limit).Take(limit).ToList();
            return Task.FromResult(new PagedCampaignsResponse<CampaignDto>
            {
                Page = page,
                Limit = limit,
                Total = _campaigns.Count,
                Data = paged
            });
        }

        public Task<PagedCampaignsResponse<VendorCampaignDto>> GetVendorCampaignsAsync(string vendorId, int page, int limit)
        {
            var all = _vendorCampaigns.ContainsKey(vendorId) ? _vendorCampaigns[vendorId] : new List<VendorCampaignDto>();
            var paged = all.Skip((page - 1) * limit).Take(limit).ToList();

            return Task.FromResult(new PagedCampaignsResponse<VendorCampaignDto>
            {
                Page = page,
                Limit = limit,
                Total = all.Count,
                Data = paged
            });
        }
    }
}
