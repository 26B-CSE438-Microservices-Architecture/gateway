// Disable Dereference of a possibly null reference warning for this test file
// CS8602: Dereference of a possibly null reference.
#pragma warning disable CS8602
using CleanArchitecture.Core.DTOs.Vendor;

using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Infrastructure.Services;
using FluentAssertions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace CleanArchitecture.UnitTests.Services
{
    public class VendorServiceTests
    {
        private readonly VendorService _vendorService;

        public VendorServiceTests()
        {
            _vendorService = new VendorService();
        }

        [Fact]
        public async Task GetVendorsAsync_ShouldReturnPagedResponse()
        {
            // Act
            var result = await _vendorService.GetVendorsAsync(1, 10);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeEmpty();
            result.Page.Should().Be(1);
        }

        [Fact]
        public async Task GetVendorByIdAsync_WithValidId_ShouldReturnVendor()
        {
            // Arrange
            var vendors = await _vendorService.GetVendorsAsync(1, 1);
            var expectedId = vendors.Data[0].Id;

            // Act
            var result = await _vendorService.GetVendorByIdAsync(expectedId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(expectedId);
        }

        [Fact]
        public async Task GetVendorByIdAsync_WithInvalidId_ShouldThrowNotFoundException()
        {
            // Act
            Func<Task> act = async () => await _vendorService.GetVendorByIdAsync("non_existent_id");

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task CreateVendorAsync_ShouldAddNewVendorAndReturnId()
        {
            // Arrange
            var request = new CreateVendorDto
            {
                Name = "Test Restaurant",
                Description = "Test Description",
                MinOrderAmount = 100,
                DeliveryFee = 15.5
            };

            // Act
            var id = await _vendorService.CreateVendorAsync(request);
            var vendor = await _vendorService.GetVendorByIdAsync(id);

            // Assert
            id.Should().StartWith("vendor_");
            vendor.Name.Should().Be(request.Name);
            vendor.DeliveryInfo.MinimumBasketAmount.Should().Be(request.MinOrderAmount);
        }

        [Fact]
        public async Task UpdateVendorStatusAsync_ToClosed_ShouldSetIsOpenToFalse()
        {
            // Arrange
            var vendors = await _vendorService.GetVendorsAsync(1, 1);
            var vendorId = vendors.Data[0].Id;
            var request = new UpdateStatusDto { Status = "Closed" };

            // Act
            await _vendorService.UpdateVendorStatusAsync(vendorId, request);
            var updatedVendor = await _vendorService.GetVendorByIdAsync(vendorId);

            // Assert
            updatedVendor.Status.Should().Be("Closed");
            updatedVendor.WorkingHours.IsOpen.Should().BeFalse();
        }

        [Fact]
        public async Task CreateCategoryAsync_ShouldAddSectionToVendor()
        {
            // Arrange
            var vendors = await _vendorService.GetVendorsAsync(1, 1);
            var vendorId = vendors.Data[0].Id;
            var request = new CreateCategoryDto { Name = "New Category" };

            // Act
            var categoryId = await _vendorService.CreateCategoryAsync(vendorId, request);
            var vendor = await _vendorService.GetVendorByIdAsync(vendorId);

            // Assert
            categoryId.Should().StartWith("cat_");
            vendor.MenuSections.Should().Contain(x => x.Id == categoryId && x.Title == "New Category");
        }

        [Fact]
        public async Task CreateProductAsync_ShouldAddProductToCategory()
        {
            // Arrange
            var vendors = await _vendorService.GetVendorsAsync(1, 1);
            var vendorId = vendors.Data[0].Id;
            var categoryId = "section_1"; // Default section in mock data
            var request = new CreateProductDto
            {
                Name = "New Burger",
                Price = 250,
                Description = "Yummy burger"
            };

            // Act
            var productId = await _vendorService.CreateProductAsync(categoryId, request);
            var vendor = await _vendorService.GetVendorByIdAsync(vendorId);

            // Assert
            productId.Should().StartWith("prod_");
            var section = vendor.MenuSections.Find(x => x.Id == categoryId);
            section.Products.Should().Contain(x => x.Id == productId && x.Name == "New Burger");
        }

        [Fact]
        public async Task UpdateProductStockAsync_ToUnavailable_ShouldUpdateIsAvailable()
        {
            // Arrange
            var productId = "prod_1"; // Default product
            var request = new UpdateStockDto { IsAvailable = false };

            // Act
            await _vendorService.UpdateProductStockAsync(productId, request);
            var vendor = await _vendorService.GetVendorByIdAsync("vendor_101");
            var product = vendor.MenuSections[0].Products.Find(x => x.Id == productId);

            // Assert
            product.IsAvailable.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteVendorAsync_ShouldRemoveFromList()
        {
            // Arrange
            var id = await _vendorService.CreateVendorAsync(new CreateVendorDto { Name = "To Be Deleted" });

            // Act
            var result = await _vendorService.DeleteVendorAsync(id);

            // Assert
            result.Should().BeTrue();
            Func<Task> act = async () => await _vendorService.GetVendorByIdAsync(id);
            await act.Should().ThrowAsync<NotFoundException>();
        }
    }
}