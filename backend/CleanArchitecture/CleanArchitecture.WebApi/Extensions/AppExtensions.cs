using CleanArchitecture.WebApi.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.OpenApi.Models;
using System.Collections.Generic;

namespace CleanArchitecture.WebApi.Extensions
{
    public static class AppExtensions
    {
        public static void UseSwaggerExtension(this IApplicationBuilder app)
        {
            app.UseSwagger(c =>
            {
                c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
                {
                    var prefix = httpReq.Headers["X-Forwarded-Prefix"].ToString();
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        swaggerDoc.Servers = new List<OpenApiServer> { new OpenApiServer { Url = prefix } };
                    }
                    else
                    {
                        swaggerDoc.Servers = new List<OpenApiServer> { new OpenApiServer { Url = "/cse-438" } };
                    }
                });
            });
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("./v1/swagger.json", "Gateway & Auth Service");
            });
        }
        public static void UseErrorHandlingMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<ErrorHandlerMiddleware>();
        }
    }
}

