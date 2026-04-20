using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Wrappers;
using CleanArchitecture.Core.Settings;
using CleanArchitecture.Infrastructure.Contexts;
using CleanArchitecture.Infrastructure.Models;
using CleanArchitecture.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Text;

namespace CleanArchitecture.Infrastructure
{
    public static class ServiceRegistration
    {
        public static void AddPersistenceInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            if (configuration.GetValue<bool>("UseInMemoryDatabase"))
            {
                // Keep the InMemory database name configurable so tests can isolate data per host and avoid cross-test collisions.
                var inMemoryDatabaseName = configuration["InMemoryDatabaseName"] ?? "ApplicationDb";
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase(inMemoryDatabaseName));
            }
            else
            {
                services.AddDbContext<ApplicationDbContext>(options =>
               options.UseNpgsql(
                   configuration.GetConnectionString("DefaultConnection"),
                   b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
            }

            services.AddIdentity<ApplicationUser, IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
            #region Services
            services.AddTransient<IAccountService, AccountService>();
            services.AddTransient<IUserService, UserService>();
            services.AddTransient<IHomeService, HomeService>();
            services.AddTransient<IVendorService, VendorService>();
            services.AddTransient<IOrderService, OrderService>();
            services.AddTransient<IPaymentService, PaymentService>();
            services.AddTransient<ICampaignService, CampaignService>();
            services.AddTransient<IReviewService, ReviewService>();
            services.AddTransient<ISearchService, SearchService>();
            services.AddTransient<IAdminService, AdminService>();
            #endregion
            services.Configure<JWTSettings>(configuration.GetSection("JWTSettings"));
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(o =>
                {
                    o.RequireHttpsMetadata = false;
                    o.SaveToken = false;
                    o.MapInboundClaims = false;
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(5),
                        ValidIssuer = configuration["JWTSettings:Issuer"],
                        ValidAudience = configuration["JWTSettings:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWTSettings:Key"])),
                        RoleClaimType = "roles"
                    };
                    o.Events = new JwtBearerEvents()
                    {
                        OnAuthenticationFailed = c =>
                        {
                            Serilog.Log.Warning("Authentication failed for {Path} from IP {IpAddress}: {Error}",
                                c.Request.Path, c.HttpContext.Connection.RemoteIpAddress, c.Exception.Message);
                            c.NoResult();
                            c.Response.StatusCode = 401;
                            c.Response.ContentType = "application/json";
                            return c.Response.WriteAsync("{\"error\":\"AUTHENTICATION_FAILED\",\"message\":\"Token validation failed.\"}");
                        },
                        OnChallenge = context =>
                        {
                            Serilog.Log.Warning("401 Unauthorized attempt on {Path} from IP {IpAddress}",
                                context.Request.Path, context.HttpContext.Connection.RemoteIpAddress);
                            context.HandleResponse();
                            context.Response.StatusCode = 401;
                            context.Response.ContentType = "application/json";
                            return context.Response.WriteAsync("{\"error\":\"UNAUTHORIZED\",\"message\":\"You are not authorized.\"}");
                        },
                        OnForbidden = context =>
                        {
                            Serilog.Log.Warning("403 Forbidden attempt on {Path} from IP {IpAddress}, User: {UserId}",
                                context.Request.Path, context.HttpContext.Connection.RemoteIpAddress,
                                context.HttpContext.User.FindFirst("uid")?.Value ?? "unknown");
                            context.Response.StatusCode = 403;
                            context.Response.ContentType = "application/json";
                            return context.Response.WriteAsync("{\"error\":\"FORBIDDEN\",\"message\":\"You are not authorized to access this resource.\"}");
                        },
                    };
                });




            services.Configure<MailSettings>(configuration.GetSection("MailSettings"));
            services.AddTransient<IDateTimeService, DateTimeService>();
            services.AddTransient<IEmailService, EmailService>();
        }
    }
}
