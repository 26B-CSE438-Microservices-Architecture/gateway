using CleanArchitecture.Core.DTOs.Admin;
using CleanArchitecture.Core.Enums;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Services
{
    public class AdminService : IAdminService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<PagedUsersResponse> GetAllUsersAsync(int page, int limit)
        {
            var allUsers = _userManager.Users.ToList();
            var totalCount = allUsers.Count;

            var pagedUsers = allUsers
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToList();

            var userDtos = new List<AdminUserDto>();
            foreach (var user in pagedUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(new AdminUserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = $"{user.FirstName} {user.LastName}".Trim(),
                    PhoneNumber = user.PhoneNumber,
                    Roles = roles.ToList(),
                    EmailConfirmed = user.EmailConfirmed
                });
            }

            return new PagedUsersResponse
            {
                Users = userDtos,
                TotalCount = totalCount,
                Page = page,
                Limit = limit
            };
        }

        public async Task<AssignRoleResponse> AssignRoleAsync(string userId, AssignRoleRequest request)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new NotFoundException("USER_NOT_FOUND", $"User with id '{userId}' was not found.");

            // Validate that the role exists in our enum
            if (!Enum.TryParse<Roles>(request.Role, ignoreCase: true, out var role))
                throw new ApiException($"Invalid role '{request.Role}'. Valid roles: {string.Join(", ", Enum.GetNames(typeof(Roles)))}");

            var roleString = role.ToString();

            // Ensure the role exists in Identity
            if (!await _roleManager.RoleExistsAsync(roleString))
                throw new ApiException($"Role '{roleString}' does not exist in the system.");

            // Check if user already has this role
            if (await _userManager.IsInRoleAsync(user, roleString))
                throw new ApiException($"User already has the role '{roleString}'.");

            var result = await _userManager.AddToRoleAsync(user, roleString);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new ApiException($"Failed to assign role: {errors}");
            }

            return new AssignRoleResponse
            {
                Message = $"Role '{roleString}' assigned successfully.",
                UserId = userId,
                AssignedRole = roleString
            };
        }
    }
}
