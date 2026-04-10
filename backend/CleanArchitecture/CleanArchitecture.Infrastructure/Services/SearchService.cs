using CleanArchitecture.Core.DTOs.Search;
using CleanArchitecture.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Services
{
    public class SearchService : ISearchService
    {
        private static readonly List<SearchVendorDto> _allVendors = new List<SearchVendorDto>
        {
            new SearchVendorDto { Id = "vendor_101", Name = "Burger Point", Rating = 4.7, IsSponsored = true, ImageUrl = "https://cdn.app.com/burgerpoint.jpg", Eta = "20-30 dk" },
            new SearchVendorDto { Id = "vendor_102", Name = "Pizza Express", Rating = 4.5, IsSponsored = false, ImageUrl = "https://cdn.app.com/pizzaexpress.jpg", Eta = "25-35 dk" },
            new SearchVendorDto { Id = "vendor_103", Name = "Komagene Çið Köfte", Rating = 4.3, IsSponsored = false, ImageUrl = "https://cdn.app.com/komagene.jpg", Eta = "15-25 dk" },
            new SearchVendorDto { Id = "vendor_104", Name = "Domino's Pizza", Rating = 4.4, IsSponsored = true, ImageUrl = "https://cdn.app.com/dominos.jpg", Eta = "30-40 dk" },
            new SearchVendorDto { Id = "vendor_105", Name = "Burger King", Rating = 4.2, IsSponsored = false, ImageUrl = "https://cdn.app.com/bk.jpg", Eta = "20-30 dk" }
        };

        private static readonly List<SearchProductDto> _allProducts = new List<SearchProductDto>
        {
            new SearchProductDto { Id = "prod_1", Name = "Double Smash Burger", PriceLabel = "210,00 TL", VendorName = "Burger Point" },
            new SearchProductDto { Id = "prod_2", Name = "Classic Burger", PriceLabel = "160,00 TL", VendorName = "Burger Point" },
            new SearchProductDto { Id = "prod_4", Name = "Margherita", PriceLabel = "180,00 TL", VendorName = "Pizza Express" },
            new SearchProductDto { Id = "prod_5", Name = "Pepperoni Pizza", PriceLabel = "200,00 TL", VendorName = "Pizza Express" }
        };

        public Task<DiscoveryResponse> GetDiscoveryAsync()
        {
            var response = new DiscoveryResponse
            {
                Sections = new List<DiscoverySectionDto>
                {
                    new DiscoverySectionDto
                    {
                        Title = "Zincir Restoranlar",
                        Type = "HORIZONTAL_LIST",
                        Items = new List<DiscoveryItemDto>
                        {
                            new DiscoveryItemDto { Id = "v_1", Name = "Komagene", LogoUrl = "https://cdn.app.com/logos/komagene.png" },
                            new DiscoveryItemDto { Id = "v_2", Name = "Domino's Pizza", LogoUrl = "https://cdn.app.com/logos/dominos.png" },
                            new DiscoveryItemDto { Id = "v_3", Name = "Burger King", LogoUrl = "https://cdn.app.com/logos/bk.png" },
                            new DiscoveryItemDto { Id = "v_4", Name = "Subway", LogoUrl = "https://cdn.app.com/logos/subway.png" }
                        }
                    },
                    new DiscoverySectionDto
                    {
                        Title = "Mutfaklar",
                        Type = "GRID",
                        Items = new List<DiscoveryItemDto>
                        {
                            new DiscoveryItemDto { Id = "cat_1", Name = "Döner", ImageUrl = "https://cdn.app.com/cuisines/doner.png", ColorCode = "#FDECEC" },
                            new DiscoveryItemDto { Id = "cat_2", Name = "Hamburger", ImageUrl = "https://cdn.app.com/cuisines/burger.png", ColorCode = "#FFF4E5" },
                            new DiscoveryItemDto { Id = "cat_3", Name = "Çið Köfte", ImageUrl = "https://cdn.app.com/cuisines/cigkofte.png", ColorCode = "#EEF7ED" },
                            new DiscoveryItemDto { Id = "cat_4", Name = "Pizza", ImageUrl = "https://cdn.app.com/cuisines/pizza.png", ColorCode = "#EEF0FD" },
                            new DiscoveryItemDto { Id = "cat_5", Name = "Sushi", ImageUrl = "https://cdn.app.com/cuisines/sushi.png", ColorCode = "#FDF4EE" }
                        }
                    }
                }
            };

            return Task.FromResult(response);
        }

        public Task<SearchResponse> SearchAsync(string keyword, double? lat, double? lng)
        {
            var lowerKeyword = keyword?.ToLower() ?? "";

            var matchedVendors = _allVendors
                .Where(v => v.Name.ToLower().Contains(lowerKeyword))
                .ToList();

            var matchedProducts = _allProducts
                .Where(p => p.Name.ToLower().Contains(lowerKeyword))
                .ToList();

            return Task.FromResult(new SearchResponse
            {
                TotalCount = matchedVendors.Count + matchedProducts.Count,
                Vendors = matchedVendors,
                Products = matchedProducts
            });
        }
    }
}
