using CleanArchitecture.Core.DTOs.User;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Infrastructure.Models;
using CleanArchitecture.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CleanArchitecture.UnitTests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStore.Object, null, null, null, null, null, null, null, null);

            _userService = new UserService(_userManagerMock.Object);
        }

        [Fact]
        public async Task GetProfileAsync_WithValidUser_ShouldReturnProfile()
        {
            // Arrange
            var userId = "user-123";
            var testUser = new ApplicationUser
            {
                Id = userId,
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                PhoneNumber = "1234567890"
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(testUser);
            _userManagerMock.Setup(x => x.GetRolesAsync(testUser)).ReturnsAsync(new List<string> { "CUSTOMER" });

            // Act
            var result = await _userService.GetProfileAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("John Doe");
            result.Email.Should().Be("john@example.com");
            result.Phone.Should().Be("1234567890");
        }

        [Fact]
        public async Task UpdateProfileAsync_ShouldOnlyUpdateNameAndPhone()
        {
            // Arrange
            var userId = "user-123";
            var testUser = new ApplicationUser
            {
                Id = userId,
                FirstName = "Old Name",
                Email = "john@example.com",
                PhoneNumber = "111"
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(testUser);
            _userManagerMock.Setup(x => x.GetRolesAsync(testUser)).ReturnsAsync(new List<string> { "CUSTOMER" });
            _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

            var request = new UserUpdateProfileRequest { Name = "New Name", Phone = "222" };

            // Act
            var result = await _userService.UpdateProfileAsync(userId, request);

            // Assert
            testUser.FirstName.Should().Be("New Name");
            testUser.PhoneNumber.Should().Be("222");
            testUser.Email.Should().Be("john@example.com"); // Should not change
        }

        [Fact]
        public async Task AddressOperations_ShouldBeSuccessful()
        {
            // Arrange
            var userId = "user-abc";
            var request = new CreateAddressRequest
            {
                Label = "Work",
                City = "Ankara",
                Street = "Plaza St.",
                PostalCode = "06000",
                Lat = 39.9,
                Lng = 32.8
            };

            // Act: Create
            var created = await _userService.CreateAddressAsync(userId, request);
            
            // Act: Get
            var addresses = await _userService.GetAddressesAsync(userId);
            
            // Assert
            created.Should().NotBeNull();
            created.Label.Should().Be("Work");
            addresses.Should().Contain(a => a.Id == created.Id);

            // Act: Delete
            await _userService.DeleteAddressAsync(userId, created.Id);
            var addressesAfterDelete = await _userService.GetAddressesAsync(userId);
            
            // Assert
            addressesAfterDelete.Should().NotContain(a => a.Id == created.Id);
        }

        [Fact]
        public async Task Favorites_ShouldBeStoredAndRetrievedCorrectly()
        {
            // Arrange
            var userId = "user-fav";
            var vendorId = "vendor_999";

            // Act: Add
            await _userService.AddFavoriteAsync(userId, vendorId);
            
            // Act: Get
            var favorites = await _userService.GetFavoritesAsync(userId, 1, 10);
            
            // Assert
            favorites.Total.Should().BeGreaterThanOrEqualTo(1);
            favorites.Data.Should().Contain(f => f.VendorId == vendorId);

            // Act: Remove
            await _userService.RemoveFavoriteAsync(userId, vendorId);
            var favoritesAfterRemove = await _userService.GetFavoritesAsync(userId, 1, 10);
            
            // Assert
            favoritesAfterRemove.Data.Should().NotContain(f => f.VendorId == vendorId);
        }
    }
}
