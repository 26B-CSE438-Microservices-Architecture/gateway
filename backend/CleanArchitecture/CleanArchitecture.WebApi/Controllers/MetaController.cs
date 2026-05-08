using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CleanArchitecture.WebApi.Controllers
{
    public class MetaController : BaseApiController
    {
        /// <summary>
        /// Retrieves system information and version details.
        /// </summary>
        [HttpGet("info")]
        public ActionResult Info()
        {
            var assembly = typeof(Program).Assembly;

            var lastUpdate = System.IO.File.GetLastWriteTime(assembly.Location);
            var version = FileVersionInfo.GetVersionInfo(assembly.Location).ProductVersion;

            return Ok(new { version, lastUpdate });
        }
    }
}
