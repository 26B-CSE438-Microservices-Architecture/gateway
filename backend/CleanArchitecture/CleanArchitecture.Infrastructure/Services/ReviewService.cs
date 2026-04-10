using CleanArchitecture.Core.DTOs.Review;
using CleanArchitecture.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Services
{
    public class ReviewService : IReviewService
    {
        private static readonly Dictionary<string, List<ReviewDto>> _reviews = new Dictionary<string, List<ReviewDto>>
        {
            ["vendor_101"] = new List<ReviewDto>
            {
                new ReviewDto { UserName = "Ahmet K.", Rating = 5, Comment = "Çok hýzlý geldi", Date = "2026-03-09" },
                new ReviewDto { UserName = "Ayþe M.", Rating = 4, Comment = "Güzeldi ama biraz geç geldi", Date = "2026-03-08" },
                new ReviewDto { UserName = "Mehmet S.", Rating = 5, Comment = "Harika, kesinlikle tavsiye ederim", Date = "2026-03-07" },
                new ReviewDto { UserName = "Fatma Y.", Rating = 3, Comment = "Ortalama", Date = "2026-03-06" }
            },
            ["vendor_102"] = new List<ReviewDto>
            {
                new ReviewDto { UserName = "Ali R.", Rating = 5, Comment = "Mükemmel pizza", Date = "2026-03-09" },
                new ReviewDto { UserName = "Zeynep D.", Rating = 4, Comment = "Çok lezzetli", Date = "2026-03-08" }
            }
        };

        public Task<PagedReviewsResponse> GetVendorReviewsAsync(string vendorId, int page, int limit)
        {
            var all = _reviews.ContainsKey(vendorId) ? _reviews[vendorId] : new List<ReviewDto>();
            var paged = all.Skip((page - 1) * limit).Take(limit).ToList();

            return Task.FromResult(new PagedReviewsResponse
            {
                Page = page,
                Limit = limit,
                Total = all.Count,
                Data = paged
            });
        }
    }
}
