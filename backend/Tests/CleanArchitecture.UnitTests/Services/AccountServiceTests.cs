// Disable Nullable Reference Types warnings for this test file
// CS8625: Cannot convert null literal to non-nullable reference type.
// CS8600: Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8625, CS8600

using CleanArchitecture.Core.DTOs.Account;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Settings;
using CleanArchitecture.Infrastructure.Contexts;
using CleanArchitecture.Infrastructure.Models;
using CleanArchitecture.Infrastructure.Services;
using CleanArchitecture.Core.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace CleanArchitecture.UnitTests.Services
{
    public class AccountServiceTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
        private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<IDateTimeService> _dateTimeServiceMock;
        private readonly IOptions<JWTSettings> _jwtSettings;
        private readonly ApplicationDbContext _dbContext;
        private readonly AccountService _accountService;

        public AccountServiceTests()
        {
            // Mock UserManager
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStore.Object, null, null, null, null, null, null, null, null);

            // Mock RoleManager
            var roleStore = new Mock<IRoleStore<IdentityRole>>();
            _roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                roleStore.Object, null, null, null, null);

            // Mock SignInManager
            _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
                _userManagerMock.Object,
                new Mock<IHttpContextAccessor>().Object,
                new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>().Object,
                null, null, null, null);

            _emailServiceMock = new Mock<IEmailService>();
            _dateTimeServiceMock = new Mock<IDateTimeService>();

            _jwtSettings = Options.Create(new JWTSettings
            {
                Key = "5b1b4b12330f40d8924b22c7302409df5b1b4b12330f40d8924b22c7302409df",
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                DurationInMinutes = 60
            });

            // InMemory DbContext for RefreshToken operations
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new ApplicationDbContext(options, _dateTimeServiceMock.Object, 
                new Mock<IAuthenticatedUserService>().Object);

            _accountService = new AccountService(
                _userManagerMock.Object,
                _roleManagerMock.Object,
                _jwtSettings,
                _dateTimeServiceMock.Object,
                _signInManagerMock.Object,
                _emailServiceMock.Object,
                _dbContext);
        }

        // ========================
        // LOGIN TESTS
        // ========================

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ShouldReturnTokenAndUser()
        {
            // Arrange
            var testUser = new ApplicationUser
            {
                Id = "user-123",
                Email = "test@example.com",
                UserName = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                EmailConfirmed = true,
                RefreshTokens = new List<RefreshToken>()
            };

            // Setup: FindByEmail via Users (Include chain) — we mock SingleOrDefaultAsync
            var users = new List<ApplicationUser> { testUser }.AsQueryable();

            _userManagerMock.Setup(x => x.Users)
                .Returns(new TestAsyncEnumerable<ApplicationUser>(users));

            _signInManagerMock.Setup(x => x.PasswordSignInAsync(
                testUser.UserName, "Password123!", false, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            _userManagerMock.Setup(x => x.GetClaimsAsync(testUser))
                .ReturnsAsync(new List<System.Security.Claims.Claim>());

            _userManagerMock.Setup(x => x.GetRolesAsync(testUser))
                .ReturnsAsync(new List<string> { "Customer" });

            _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            var request = new AuthenticationRequest
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            // Act
            var result = await _accountService.LoginAsync(request, "127.0.0.1");

            // Assert
            result.Should().NotBeNull();
            result.JWToken.Should().NotBeNullOrEmpty();
            result.RefreshToken.Should().NotBeNullOrEmpty();
            result.User.Should().NotBeNull();
            result.User.Email.Should().Be("test@example.com");
            result.ExpiresIn.Should().Be(3600);
        }

        [Fact]
        public async Task LoginAsync_WithNonExistentEmail_ShouldThrowApiException()
        {
            // Arrange
            var emptyUsers = new List<ApplicationUser>().AsQueryable();
            _userManagerMock.Setup(x => x.Users)
                .Returns(new TestAsyncEnumerable<ApplicationUser>(emptyUsers));

            var request = new AuthenticationRequest
            {
                Email = "nonexistent@example.com",
                Password = "Password123!"
            };

            // Act & Assert
            var act = () => _accountService.LoginAsync(request, "127.0.0.1");
            await act.Should().ThrowAsync<ApiException>()
                .WithMessage("*No Accounts Registered*");
        }

        [Fact]
        public async Task LoginAsync_WithWrongPassword_ShouldThrowApiException()
        {
            // Arrange
            var testUser = new ApplicationUser
            {
                Id = "user-123",
                Email = "test@example.com",
                UserName = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                EmailConfirmed = true,
                RefreshTokens = new List<RefreshToken>()
            };

            var users = new List<ApplicationUser> { testUser }.AsQueryable();
            _userManagerMock.Setup(x => x.Users)
                .Returns(new TestAsyncEnumerable<ApplicationUser>(users));

            _signInManagerMock.Setup(x => x.PasswordSignInAsync(
                testUser.UserName, "WrongPassword", false, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            var request = new AuthenticationRequest
            {
                Email = "test@example.com",
                Password = "WrongPassword"
            };

            // Act & Assert
            var act = () => _accountService.LoginAsync(request, "127.0.0.1");
            await act.Should().ThrowAsync<ApiException>()
                .WithMessage("*Invalid Credentials*");
        }

        // ========================
        // REGISTER TESTS
        // ========================

        [Fact]
        public async Task RegisterAsync_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            _userManagerMock.Setup(x => x.FindByEmailAsync("new@example.com"))
                .ReturnsAsync((ApplicationUser)null);

            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), "SecurePass1!"))
                .ReturnsAsync(IdentityResult.Success);

            _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Customer"))
                .ReturnsAsync(IdentityResult.Success);

            var request = new RegisterRequest
            {
                Email = "new@example.com",
                Password = "SecurePass1!",
                Name = "New",
                Surname = "User"
            };

            // Act
            var result = await _accountService.RegisterAsync(request, "https://localhost");

            // Assert
            result.Should().NotBeNull();
            result.Message.Should().Contain("registered successfully");
            result.UserId.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task RegisterAsync_WithExistingEmail_ShouldThrowApiException()
        {
            // Arrange
            var existingUser = new ApplicationUser { Email = "existing@example.com" };
            _userManagerMock.Setup(x => x.FindByEmailAsync("existing@example.com"))
                .ReturnsAsync(existingUser);

            var request = new RegisterRequest
            {
                Email = "existing@example.com",
                Password = "SecurePass1!",
                Name = "Existing",
                Surname = "User"
            };

            // Act & Assert
            var act = () => _accountService.RegisterAsync(request, "https://localhost");
            await act.Should().ThrowAsync<ApiException>()
                .WithMessage("*already registered*");
        }

        // ========================
        // CHANGE PASSWORD TESTS
        // ========================

        [Fact]
        public async Task ChangePasswordAsync_WithCorrectOldPassword_ShouldReturnTrue()
        {
            // Arrange
            var testUser = new ApplicationUser { Id = "user-123" };
            _userManagerMock.Setup(x => x.FindByIdAsync("user-123")).ReturnsAsync(testUser);
            _userManagerMock.Setup(x => x.ChangePasswordAsync(testUser, "OldPass1!", "NewPass1!"))
                .ReturnsAsync(IdentityResult.Success);

            var request = new ChangePasswordRequest
            {
                OldPassword = "OldPass1!",
                NewPassword = "NewPass1!"
            };

            // Act
            var result = await _accountService.ChangePasswordAsync("user-123", request);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ChangePasswordAsync_WithNonExistentUser_ShouldThrowApiException()
        {
            // Arrange
            _userManagerMock.Setup(x => x.FindByIdAsync("unknown-id"))
                .ReturnsAsync((ApplicationUser)null);

            var request = new ChangePasswordRequest
            {
                OldPassword = "OldPass1!",
                NewPassword = "NewPass1!"
            };

            // Act & Assert
            var act = () => _accountService.ChangePasswordAsync("unknown-id", request);
            await act.Should().ThrowAsync<ApiException>()
                .WithMessage("User not found.");
        }

        // ========================
        // DELETE ACCOUNT TESTS
        // ========================

        [Fact(Skip = "ExecuteDeleteAsync is not supported by InMemory provider. Tested in Integration Tests instead.")]
        public async Task DeleteAccountAsync_WithCorrectPassword_ShouldReturnTrue()
        {
            // Arrange
            var testUser = new ApplicationUser { Id = "user-456" };
            _userManagerMock.Setup(x => x.FindByIdAsync("user-456")).ReturnsAsync(testUser);
            _userManagerMock.Setup(x => x.CheckPasswordAsync(testUser, "MyPassword!")).ReturnsAsync(true);
            _userManagerMock.Setup(x => x.DeleteAsync(testUser)).ReturnsAsync(IdentityResult.Success);

            var request = new DeleteAccountRequest { Password = "MyPassword!" };

            // Act
            var result = await _accountService.DeleteAccountAsync("user-456", request);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteAccountAsync_WithWrongPassword_ShouldThrowApiException()
        {
            // Arrange
            var testUser = new ApplicationUser { Id = "user-456" };
            _userManagerMock.Setup(x => x.FindByIdAsync("user-456")).ReturnsAsync(testUser);
            _userManagerMock.Setup(x => x.CheckPasswordAsync(testUser, "WrongPass")).ReturnsAsync(false);

            var request = new DeleteAccountRequest { Password = "WrongPass" };

            // Act & Assert
            var act = () => _accountService.DeleteAccountAsync("user-456", request);
            await act.Should().ThrowAsync<ApiException>()
                .WithMessage("Invalid password.");
        }
    }
}
