using CleanArchitecture.Core.DTOs.Search;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Interfaces
{
    public interface ISearchService
    {
        Task<DiscoveryResponse> GetDiscoveryAsync();
        Task<SearchResponse> SearchAsync(string keyword, double? lat, double? lng);
    }
}
