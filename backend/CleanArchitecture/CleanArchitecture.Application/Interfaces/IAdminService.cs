using CleanArchitecture.Core.DTOs.Admin;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Interfaces
{
    public interface IAdminService
    {
        Task<PagedUsersResponse> GetAllUsersAsync(int page, int limit);
        Task<AssignRoleResponse> AssignRoleAsync(string userId, AssignRoleRequest request);
    }
}
