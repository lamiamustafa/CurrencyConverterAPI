using CurrencyConverter.API.Controllers;
using CurrencyConverter.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace CurrencyConverter.Tests.UnitTests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<UserManager<IdentityUser>> _mockUserManager;
        private readonly Mock<SignInManager<IdentityUser>> _mockSignInManager;
        private readonly Mock<IConfiguration> _mockConfig;

        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockUserManager = new Mock<UserManager<IdentityUser>>(
                Mock.Of<IUserStore<IdentityUser>>(), null, null, null, null, null, null, null, null);

            _mockSignInManager = new Mock<SignInManager<IdentityUser>>(
                _mockUserManager.Object,
                Mock.Of<Microsoft.AspNetCore.Http.IHttpContextAccessor>(),
                Mock.Of<IUserClaimsPrincipalFactory<IdentityUser>>(),
                null, null, null, null);

            _mockConfig = new Mock<IConfiguration>();

            _controller = new AuthController(_mockSignInManager.Object, _mockUserManager.Object, _mockConfig.Object);
        }

        [Fact]
        public async Task Login_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            var loginRequest = new LoginRequest { UserName = "testuser", Password = "password" };
            _mockUserManager.Setup(m => m.FindByNameAsync(loginRequest.UserName)).ReturnsAsync((IdentityUser)null);

            // Act
            var result = await _controller.Login(loginRequest);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Login_InvalidPassword_ReturnsUnauthorized()
        {
            // Arrange
            var user = new IdentityUser { UserName = "testuser", Id = "123" };
            var loginRequest = new LoginRequest { UserName = "testuser", Password = "wrongpass" };

            _mockUserManager.Setup(m => m.FindByNameAsync(loginRequest.UserName)).ReturnsAsync(user);
            _mockSignInManager.Setup(m => m.PasswordSignInAsync(loginRequest.UserName, loginRequest.Password, false, false))
                .ReturnsAsync(SignInResult.Failed);

            // Act
            var result = await _controller.Login(loginRequest);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsToken()
        {
            // Arrange
            var user = new IdentityUser { UserName = "testuser", Id = "123" };
            var loginRequest = new LoginRequest { UserName = "testuser", Password = "correctpass" };
            var roles = new List<string> { "Admin" };

            _mockUserManager.Setup(m => m.FindByNameAsync(loginRequest.UserName)).ReturnsAsync(user);
            _mockSignInManager.Setup(m => m.PasswordSignInAsync(loginRequest.UserName, loginRequest.Password, false, false))
                .ReturnsAsync(SignInResult.Success);
            _mockUserManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(roles);

            // Mock config values
            _mockConfig.Setup(c => c["JwtSettings:Key"]).Returns("supersecretkey1234567890supersecretkey1234567890");
            _mockConfig.Setup(c => c["JwtSettings:Issuer"]).Returns("TestIssuer");
            _mockConfig.Setup(c => c["JwtSettings:Audience"]).Returns("TestAudience");
            _mockConfig.Setup(c => c["JwtSettings:ExpiryMinutes"]).Returns("30");

            // Act
            var result = await _controller.Login(loginRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = okResult.Value;

            var tokenProperty = value.GetType().GetProperty("token");
            Assert.NotNull(tokenProperty);

            var tokenValue = tokenProperty.GetValue(value) as string;
            Assert.False(string.IsNullOrWhiteSpace(tokenValue));
        }
    }
}