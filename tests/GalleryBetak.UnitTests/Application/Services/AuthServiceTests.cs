using GalleryBetak.Application.Common;
using GalleryBetak.Application.DTOs.Auth;
using GalleryBetak.Application.Interfaces;
using GalleryBetak.Domain.Entities;
using GalleryBetak.Domain.Interfaces;
using GalleryBetak.Infrastructure.Identity;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace GalleryBetak.UnitTests.Application.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
        private readonly Mock<IJwtTokenService> _mockJwtTokenService;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<ILogger<AuthService>> _mockLogger;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
                _mockUserManager.Object,
                contextAccessor.Object,
                claimsFactory.Object,
                null!, null!, null!, null!);

            _mockConfig = new Mock<IConfiguration>();
            _mockJwtTokenService = new Mock<IJwtTokenService>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<AuthService>>();

            _authService = new AuthService(
                _mockUserManager.Object,
                _mockSignInManager.Object,
                _mockJwtTokenService.Object,
                _mockUnitOfWork.Object,
                _mockConfig.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsSuccess()
        {
            // Arrange
            var request = new LoginRequest { Email = "test@test.com", Password = "Password123!" };
            var user = ApplicationUser.Create(request.Email, "First", "Last");
            
            _mockUserManager.Setup(m => m.FindByEmailAsync(request.Email))
                .ReturnsAsync(user);
            
            _mockSignInManager.Setup(m => m.CheckPasswordSignInAsync(user, request.Password, true))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            _mockUserManager.Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Customer" });

            _mockJwtTokenService.Setup(j => j.GenerateAccessToken(user, It.IsAny<IList<string>>()))
                .Returns(("access_token", DateTime.UtcNow.AddHours(1)));

            _mockConfig.Setup(c => c["JwtSettings:RefreshTokenExpirationDays"]).Returns("7");

            // Act
            var response = await _authService.LoginAsync(request);

            // Assert
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.AccessToken.Should().Be("access_token");
        }

        [Fact]
        public async Task LoginAsync_NonExistentUser_ReturnsUnauthorized()
        {
            // Arrange
            var request = new LoginRequest { Email = "wrong@test.com", Password = "any" };
            _mockUserManager.Setup(m => m.FindByEmailAsync(request.Email))
                .ReturnsAsync((ApplicationUser)null!);

            // Act
            var response = await _authService.LoginAsync(request);

            // Assert
            response.Success.Should().BeFalse();
            response.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task RegisterAsync_EmailAlreadyExists_ReturnsConflict()
        {
            // Arrange
            var request = new RegisterRequest { Email = "exists@test.com", Password = "Pass" };
            _mockUserManager.Setup(m => m.FindByEmailAsync(request.Email))
                .ReturnsAsync(ApplicationUser.Create(request.Email, "F", "L"));

            // Act
            var response = await _authService.RegisterAsync(request);

            // Assert
            response.Success.Should().BeFalse();
            response.StatusCode.Should().Be(409);
        }
    }
}

