using CleanArchitecture.Core.DTOs.Review;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Interfaces
{
    public interface IReviewService
    {
        Task<PagedReviewsResponse> GetVendorReviewsAsync(string vendorId, int page, int limit);
    }
}
