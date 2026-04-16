using CleanArchitecture.Core;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Infrastructure;
using CleanArchitecture.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using CleanArchitecture.WebApi.Extensions;
using CleanArchitecture.WebApi.Grpc;
using CleanArchitecture.WebApi.Middlewares;
using CleanArchitecture.WebApi.Services;
using Yarp.ReverseProxy.Transforms;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

//Add configurations
builder.Configuration.AddJsonFile("appsettings.json");
builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true);
builder.Configuration.AddEnvironmentVariables();

//Initialize Logger early so all startup logs are captured
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddApplicationLayer();
builder.Services.AddPersistenceInfrastructure(builder.Configuration);
builder.Services.AddSwaggerExtension();
builder.Services.AddControllers();
builder.Services.AddApiVersioningExtension();
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection"), name: "PostgreSQL")
    .AddRedis(builder.Configuration.GetConnectionString("RedisConnection") ?? "localhost:6379,abortConnect=false", name: "Redis");
builder.Services.AddScoped<IAuthenticatedUserService, AuthenticatedUserService>();

// Rate Limiting — IP tabanlı, dakikada 100 istek limiti
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 5;
    });
    options.OnRejected = async (context, cancellationToken) =>
    {
        Log.Warning("Rate limit exceeded for IP: {IpAddress}, Path: {Path}",
            context.HttpContext.Connection.RemoteIpAddress,
            context.HttpContext.Request.Path);
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            "{\"error\":\"TOO_MANY_REQUESTS\",\"message\":\"Rate limit exceeded. Please try again later.\"}",
            cancellationToken);
    };
});

builder.Services.AddGrpc();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(builderContext =>
    {
        builderContext.AddRequestTransform(transformContext =>
        {
            if (transformContext.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                var userId = transformContext.HttpContext.User.FindFirst("uid")?.Value;
                var rolesList = System.Linq.Enumerable.Select(transformContext.HttpContext.User.FindAll("roles"), c => c.Value);
                var roles = string.Join(",", rolesList);

                if (userId != null) transformContext.ProxyRequest.Headers.TryAddWithoutValidation("X-User-Id", userId);
                if (!string.IsNullOrEmpty(roles)) transformContext.ProxyRequest.Headers.TryAddWithoutValidation("X-User-Role", roles);
            }
            return new System.Threading.Tasks.ValueTask();
        });
    });

//Build the application
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();

app.UseErrorHandlingMiddleware();

app.UseRouting();
app.UseRateLimiter();

var frontendOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:3000" };
app.UseCors(options => options.WithOrigins(frontendOrigins).AllowAnyHeader().AllowAnyMethod());

app.UseAuthentication();
app.UseAuthorization();
app.UseSwaggerExtension();
app.UseHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapGet("/", () => "Navigate to /swagger to see the API documentation.\nNavigate to /health to see the health status of the application.");

app.MapControllers();
app.MapGrpcService<AuthGrpcService>();
app.MapReverseProxy();



//Seed Default Data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var loggerFactory = services.GetRequiredService<ILoggerFactory>();
    try
    {
        var context = services.GetRequiredService<CleanArchitecture.Infrastructure.Contexts.ApplicationDbContext>();
        if (context.Database.IsRelational())
        {
            await context.Database.MigrateAsync();
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        await CleanArchitecture.Infrastructure.Seeds.DefaultRoles.SeedAsync(userManager, roleManager);
        await CleanArchitecture.Infrastructure.Seeds.DefaultSuperAdmin.SeedAsync(userManager, roleManager);
        await CleanArchitecture.Infrastructure.Seeds.DefaultBasicUser.SeedAsync(userManager, roleManager);
        Log.Information("Finished Seeding Default Data");
        Log.Information("Application Starting");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "An error occurred seeding the DB");
    }
    finally
    {
        Log.CloseAndFlush();
    }
}

//Start the application
app.Run();

// Required for WebApplicationFactory in integration tests
public partial class Program { }