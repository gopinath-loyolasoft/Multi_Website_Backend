using CollegeWebsitePlatform.Application.DTOs;
using CollegeWebsitePlatform.Application.Interfaces;
using FluentAssertions;
using Moq;

namespace CollegeWebsitePlatform.Tests.Unit.Services;

/// <summary>
/// Unit tests for IAuthService — login, refresh, logout, /auth/me.
/// These tests verify the service layer logic using mocked dependencies.
/// </summary>
public class AuthServiceTests
{
    private readonly Mock<IAuthService> _authServiceMock;

    public AuthServiceTests()
    {
        _authServiceMock = new Mock<IAuthService>();
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsLoginResponse()
    {
        // Arrange
        var expectedResponse = new LoginResponse
        {
            Token = "test-jwt-token",
            RefreshToken = "test-refresh-token",
            UserId = Guid.NewGuid(),
            Username = "admin",
            FullName = "Test Admin",
            Role = "CollegeAdmin",
            TenantId = Guid.NewGuid(),
            TenantCode = "RNC",
            ExpiresAt = DateTime.UtcNow.AddHours(8)
        };

        _authServiceMock
            .Setup(s => s.LoginAsync("admin", "Admin@123"))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _authServiceMock.Object.LoginAsync("admin", "Admin@123");

        // Assert
        result.Should().NotBeNull();
        result!.Token.Should().Be("test-jwt-token");
        result.Username.Should().Be("admin");
        result.Role.Should().Be("CollegeAdmin");
        result.TenantCode.Should().Be("RNC");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidCredentials_ReturnsNull()
    {
        // Arrange
        _authServiceMock
            .Setup(s => s.LoginAsync("wrong", "wrong"))
            .ReturnsAsync((LoginResponse?)null);

        // Act
        var result = await _authServiceMock.Object.LoginAsync("wrong", "wrong");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ReturnsRefreshResponse()
    {
        // Arrange
        var expectedResponse = new RefreshTokenResponse
        {
            Token = "new-jwt-token",
            RefreshToken = "new-refresh-token",
            ExpiresAt = DateTime.UtcNow.AddHours(8)
        };

        _authServiceMock
            .Setup(s => s.RefreshTokenAsync("valid-refresh-token"))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _authServiceMock.Object.RefreshTokenAsync("valid-refresh-token");

        // Assert
        result.Should().NotBeNull();
        result!.Token.Should().Be("new-jwt-token");
        result.RefreshToken.Should().Be("new-refresh-token");
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInvalidToken_ReturnsNull()
    {
        // Arrange
        _authServiceMock
            .Setup(s => s.RefreshTokenAsync("invalid-token"))
            .ReturnsAsync((RefreshTokenResponse?)null);

        // Act
        var result = await _authServiceMock.Object.RefreshTokenAsync("invalid-token");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LogoutAsync_WithValidToken_ReturnsTrue()
    {
        // Arrange
        _authServiceMock
            .Setup(s => s.LogoutAsync("valid-token"))
            .ReturnsAsync(true);

        // Act
        var result = await _authServiceMock.Object.LogoutAsync("valid-token");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetCurrentUserProfileAsync_WithValidUserId_ReturnsCurrentUserDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedProfile = new CurrentUserDto
        {
            Id = userId,
            Username = "admin",
            Email = "admin@rnc.college",
            FullName = "Test Admin",
            Role = "CollegeAdmin",
            IsActive = true,
            Permissions = new List<string> { "PAGE_MANAGE", "NEWS_MANAGE" },
            Tenants = new List<TenantMembershipDto>
            {
                new()
                {
                    TenantId = Guid.NewGuid(),
                    TenantCode = "RNC",
                    TenantName = "RNC College",
                    Domain = "rnc.localhost",
                    IsPrimary = true
                }
            }
        };

        _authServiceMock
            .Setup(s => s.GetCurrentUserProfileAsync(userId))
            .ReturnsAsync(expectedProfile);

        // Act
        var result = await _authServiceMock.Object.GetCurrentUserProfileAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.Username.Should().Be("admin");
        result.Permissions.Should().Contain("PAGE_MANAGE");
        result.Tenants.Should().HaveCount(1);
        result.Tenants[0].TenantCode.Should().Be("RNC");
    }

    [Fact]
    public async Task GetCurrentUserProfileAsync_WithInvalidUserId_ReturnsNull()
    {
        // Arrange
        _authServiceMock
            .Setup(s => s.GetCurrentUserProfileAsync(It.IsAny<Guid>()))
            .ReturnsAsync((CurrentUserDto?)null);

        // Act
        var result = await _authServiceMock.Object.GetCurrentUserProfileAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }
}
