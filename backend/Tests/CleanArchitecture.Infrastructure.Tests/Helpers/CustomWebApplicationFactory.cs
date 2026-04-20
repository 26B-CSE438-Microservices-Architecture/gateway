using CleanArchitecture.Infrastructure.Contexts;
using CleanArchitecture.Infrastructure.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System;
using System.Linq;

namespace CleanArchitecture.Infrastructure.Tests.Helpers
{
    /// <summary>
    /// Custom factory that triggers InMemory database mode using existing app settings 
    /// for integration testing, avoiding DB provider collisions.
    /// </summary>
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _inMemoryDatabaseName = $"ApplicationDb_Test_{Guid.NewGuid():N}";

        static CustomWebApplicationFactory()
        {
            System.Environment.SetEnvironmentVariable("UseInMemoryDatabase", "true");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["UseInMemoryDatabase"] = "true",
                    ["InMemoryDatabaseName"] = _inMemoryDatabaseName
                });
            });

            builder.ConfigureServices(services =>
            {
                var dbContextOptionsDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (dbContextOptionsDescriptor != null)
                {
                    services.Remove(dbContextOptionsDescriptor);
                }

                var dbContextDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(ApplicationDbContext));
                if (dbContextDescriptor != null)
                {
                    services.Remove(dbContextDescriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase(_inMemoryDatabaseName));

                // Safely clear HealthChecks without interfering with YARP
                services.Configure<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckServiceOptions>(options =>
                {
                    options.Registrations.Clear();
                });

                // Build the service provider & seed test data
                var sp = services.BuildServiceProvider();
                using (var scope = sp.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<ApplicationDbContext>();

                    db.Database.EnsureDeleted();
                    db.Database.EnsureCreated();

                    var roleManager = scopedServices.GetRequiredService<RoleManager<IdentityRole>>();
                    var userManager = scopedServices.GetRequiredService<UserManager<ApplicationUser>>();

                    Seeds.DefaultRoles.SeedAsync(userManager, roleManager).Wait();
                    Seeds.DefaultSuperAdmin.SeedAsync(userManager, roleManager).Wait();
                    Seeds.DefaultBasicUser.SeedAsync(userManager, roleManager).Wait();
                }
            });
        }
    }
}
