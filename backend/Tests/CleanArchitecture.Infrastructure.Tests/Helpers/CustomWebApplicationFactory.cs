using CleanArchitecture.Infrastructure.Contexts;
using CleanArchitecture.Infrastructure.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace CleanArchitecture.Infrastructure.Tests.Helpers
{
    /// <summary>
    /// Custom factory that triggers InMemory database mode using existing app settings 
    /// for integration testing, avoiding DB provider collisions.
    /// </summary>
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        static CustomWebApplicationFactory()
        {
            System.Environment.SetEnvironmentVariable("UseInMemoryDatabase", "true");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureServices(services =>
            {
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
