using CleanArchitecture.Core.DTOs.Campaign;
using CleanArchitecture.Core.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Services
{
    public class CampaignService : ICampaignService
    {
        public Task<PagedCampaignsResponse<CampaignDto>> GetCampaignsAsync(int page, int limit)
        {
            return Task.FromResult(new PagedCampaignsResponse<CampaignDto>
            {
                Page = page,
                Limit = limit,
                Total = 0,
                Data = new List<CampaignDto>()
            });
        }

        public Task<PagedCampaignsResponse<VendorCampaignDto>> GetVendorCampaignsAsync(string vendorId, int page, int limit)
        {
            return Task.FromResult(new PagedCampaignsResponse<VendorCampaignDto>
            {
                Page = page,
                Limit = limit,
                Total = 0,
                Data = new List<VendorCampaignDto>()
            });
        }
    }
}
