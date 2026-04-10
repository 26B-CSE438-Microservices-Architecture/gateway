using CleanArchitecture.Core.DTOs.Vendor;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Interfaces
{
    public interface IVendorService
    {
        Task<PagedVendorsResponse> GetVendorsAsync(int page, int limit);
        Task<VendorDetailDto> GetVendorByIdAsync(string vendorId);
    }
}
