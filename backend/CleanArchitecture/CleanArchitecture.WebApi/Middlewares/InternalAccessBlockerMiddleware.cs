using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Middlewares
{
    public class InternalAccessBlockerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<InternalAccessBlockerMiddleware> _logger;

        public InternalAccessBlockerMiddleware(RequestDelegate next, ILogger<InternalAccessBlockerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant();

            // Block any request that tries to access internal endpoints
            if (path != null && (path.StartsWith("/internal") || path.Contains("/internal/")))
            {
                _logger.LogWarning("Blocked external attempt to access internal endpoint. Path: {Path}, IP: {IpAddress}", 
                    path, context.Connection.RemoteIpAddress);
                
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"error\":\"FORBIDDEN\",\"message\":\"Access to internal endpoints is forbidden.\"}");
                return; // Short-circuit the pipeline
            }

            // Continue down the pipeline
            await _next(context);
        }
    }
}
