using CleanArchitecture.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Controllers
{
    [Route("api/v1/home")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly IHomeService _homeService;

        public HomeController(IHomeService homeService)
        {
            _homeService = homeService;
        }

        /// <summary>
        /// Retrieves the landing page discovery data (campaigns, nearby restaurants, etc.).
        /// </summary>
        [HttpGet("discover")]
        [Authorize]
        public async Task<IActionResult> GetDiscover()
        {
            var userId = User.FindFirstValue("uid");
            return Ok(await _homeService.GetDiscoverAsync(userId));
        }
    }
}
