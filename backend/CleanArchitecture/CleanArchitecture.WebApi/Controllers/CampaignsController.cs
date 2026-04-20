using CleanArchitecture.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Controllers
{
    [Route("api/v1/campaigns")]
    [ApiController]
    public class CampaignsController : ControllerBase
    {
        private readonly ICampaignService _campaignService;

        public CampaignsController(ICampaignService campaignService)
        {
            _campaignService = campaignService;
        }

        /// <summary>
        /// Retrieves a paginated list of global active campaigns and promotions.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCampaigns([FromQuery] int page = 1, [FromQuery] int limit = 20)
        {
            return Ok(await _campaignService.GetCampaignsAsync(page, limit));
        }
    }
}
