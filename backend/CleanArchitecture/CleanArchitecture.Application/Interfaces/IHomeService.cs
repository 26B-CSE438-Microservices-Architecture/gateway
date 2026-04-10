using CleanArchitecture.Core.DTOs.Home;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Interfaces
{
    public interface IHomeService
    {
        Task<DiscoverResponse> GetDiscoverAsync(string userId);
    }
}
