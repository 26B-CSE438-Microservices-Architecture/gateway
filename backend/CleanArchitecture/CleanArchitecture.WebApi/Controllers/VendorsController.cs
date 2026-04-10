using CleanArchitecture.Core.DTOs.Review;
using CleanArchitecture.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Controllers
{
    [Route("api/v1/vendors")]
    [ApiController]
    public class VendorsController : ControllerBase
    {
        private readonly IVendorService _vendorService;
        private readonly IReviewService _reviewService;
        private readonly ICampaignService _campaignService;

        public VendorsController(
            IVendorService vendorService,
            IReviewService reviewService,
            ICampaignService campaignService)
        {
            _vendorService = vendorService;
            _reviewService = reviewService;
            _campaignService = campaignService;
        }

        [HttpGet]
        public async Task<IActionResult> GetVendors([FromQuery] int page = 1, [FromQuery] int limit = 20)
        {
            return Ok(await _vendorService.GetVendorsAsync(page, limit));
        }

        [HttpGet("{vendor_id}")]
        public async Task<IActionResult> GetVendor(string vendor_id)
        {
            return Ok(await _vendorService.GetVendorByIdAsync(vendor_id));
        }

        [HttpGet("{vendor_id}/reviews")]
        public async Task<IActionResult> GetVendorReviews(
            string vendor_id,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20)
        {
            return Ok(await _reviewService.GetVendorReviewsAsync(vendor_id, page, limit));
        }

        [HttpGet("{vendor_id}/campaigns")]
        public async Task<IActionResult> GetVendorCampaigns(
            string vendor_id,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20)
        {
            return Ok(await _campaignService.GetVendorCampaignsAsync(vendor_id, page, limit));
        }
    }
}
