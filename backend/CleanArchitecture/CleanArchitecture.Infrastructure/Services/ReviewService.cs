using CleanArchitecture.Core.DTOs.Review;
using CleanArchitecture.Core.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Services
{
    public class ReviewService : IReviewService
    {
        public Task<PagedReviewsResponse> GetVendorReviewsAsync(string vendorId, int page, int limit)
        {
            return Task.FromResult(new PagedReviewsResponse
            {
                Page = page,
                Limit = limit,
                Total = 0,
                Data = new List<ReviewDto>()
            });
        }
    }
}
