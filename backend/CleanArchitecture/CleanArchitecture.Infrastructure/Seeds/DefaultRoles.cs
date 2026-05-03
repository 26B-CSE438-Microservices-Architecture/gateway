using CleanArchitecture.Core.Enums;
using CleanArchitecture.Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Seeds
{
    public static class DefaultRoles
    {
        public static async Task SeedAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            //Seed Roles
            if (!await roleManager.RoleExistsAsync(Roles.SysAdmin.ToString()))
                await roleManager.CreateAsync(new IdentityRole(Roles.SysAdmin.ToString()));

            if (!await roleManager.RoleExistsAsync(Roles.RestaurantAdmin.ToString()))
                await roleManager.CreateAsync(new IdentityRole(Roles.RestaurantAdmin.ToString()));

            if (!await roleManager.RoleExistsAsync(Roles.Courier.ToString()))
                await roleManager.CreateAsync(new IdentityRole(Roles.Courier.ToString()));

            if (!await roleManager.RoleExistsAsync(Roles.Customer.ToString()))
                await roleManager.CreateAsync(new IdentityRole(Roles.Customer.ToString()));
        }
    }
}
