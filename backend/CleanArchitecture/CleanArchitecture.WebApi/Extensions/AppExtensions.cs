using CleanArchitecture.WebApi.Middlewares;
using Microsoft.AspNetCore.Builder;

namespace CleanArchitecture.WebApi.Extensions
{
    public static class AppExtensions
    {
        public static void UseSwaggerExtension(this IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gateway & Auth Service");
                c.SwaggerEndpoint("/api/order/swagger/v1/swagger.json", "Order Service");
                c.SwaggerEndpoint("/api/restaurant/swagger/v1/swagger.json", "Restaurant Service");
                c.SwaggerEndpoint("/api/payment/swagger/v1/swagger.json", "Payment Service");
                c.SwaggerEndpoint("/api/user/swagger/v1/swagger.json", "User Service");
            });
        }
        public static void UseErrorHandlingMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<ErrorHandlerMiddleware>();
        }
    }
}
