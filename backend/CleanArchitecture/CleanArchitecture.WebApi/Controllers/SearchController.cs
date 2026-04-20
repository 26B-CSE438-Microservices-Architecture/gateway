using CleanArchitecture.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Controllers
{
    [Route("api/v1/search")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchService;

        public SearchController(ISearchService searchService)
        {
            _searchService = searchService;
        }

        [HttpGet("discovery")]
        public async Task<IActionResult> GetDiscovery()
        {
            return Ok(await _searchService.GetDiscoveryAsync());
        }

        /// <summary>
        /// Performs a global search across restaurants and products.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Search(
            [FromQuery] string q,
            [FromQuery] double? lat = null,
            [FromQuery] double? lng = null)
        {
            return Ok(await _searchService.SearchAsync(q, lat, lng));
        }
    }
}
