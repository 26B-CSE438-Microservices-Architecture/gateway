using CleanArchitecture.Core.DTOs.Vendor;
using CleanArchitecture.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Controllers
{
    [Route("internal/v1/vendors")]
    [ApiController]
    public class InternalVendorsController : ControllerBase
    {
        private readonly IVendorService _vendorService;

        public InternalVendorsController(IVendorService vendorService)
        {
            _vendorService = vendorService;
        }

        /// <summary>
        /// Bulk fetch vendor details (name, image_url) by IDs. Used by Gateway/BFF for composition.
        /// </summary>
        [HttpPost("lookup")]
        public async Task<IActionResult> BulkLookup([FromBody] VendorLookupRequest request)
        {
            var vendors = await _vendorService.LookupVendorsAsync(request.VendorIds);
            return Ok(new VendorLookupResponse { Vendors = vendors });
        }
    }
}
