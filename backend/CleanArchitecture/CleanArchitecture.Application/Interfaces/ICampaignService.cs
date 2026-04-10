using CleanArchitecture.Core.DTOs.Campaign;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Interfaces
{
    public interface ICampaignService
    {
        Task<PagedCampaignsResponse<CampaignDto>> GetCampaignsAsync(int page, int limit);
        Task<PagedCampaignsResponse<VendorCampaignDto>> GetVendorCampaignsAsync(string vendorId, int page, int limit);
    }
}
