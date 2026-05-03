using CleanArchitecture.Core.DTOs.Vendor;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Interfaces
{
    public interface IVendorService
    {
        Task<PagedVendorsResponse> GetVendorsAsync(int page, int limit);
        Task<VendorDetailDto> GetVendorByIdAsync(string vendorId);
        Task<ProductDto> GetProductByIdAsync(string productId);
        Task<PagedVendorsResponse> GetNearbyVendorsAsync(double lat, double lng, double radiusKm);
        
        // Internal Lookup
        Task<List<VendorLookupItemDto>> LookupVendorsAsync(List<string> vendorIds);
        
        // Managing Restaurants (Owner side)
        Task<string> CreateVendorAsync(CreateVendorDto request);
        Task<bool> UpdateVendorAsync(string vendorId, UpdateVendorDto request);
        Task<bool> UpdateVendorStatusAsync(string vendorId, UpdateStatusDto request);
        Task<bool> DeleteVendorAsync(string vendorId);

        // Menu & Category Management
        Task<string> CreateCategoryAsync(string vendorId, CreateCategoryDto request);
        Task<bool> UpdateCategoryAsync(string categoryId, UpdateCategoryDto request);
        Task<bool> DeleteCategoryAsync(string categoryId);

        // Product Management
        Task<string> CreateProductAsync(string categoryId, CreateProductDto request);
        Task<bool> UpdateProductAsync(string productId, UpdateProductDto request);
        Task<bool> UpdateProductStockAsync(string productId, UpdateStockDto request);
        Task<bool> DeleteProductAsync(string productId);
    }
}
