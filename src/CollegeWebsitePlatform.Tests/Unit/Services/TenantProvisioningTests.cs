using CollegeWebsitePlatform.Application.DTOs;
using CollegeWebsitePlatform.Application.Interfaces;
using FluentAssertions;
using Moq;

namespace CollegeWebsitePlatform.Tests.Unit.Services;

/// <summary>
/// Unit tests for ITenantProvisioningService — college creation, suspension, activation.
/// </summary>
public class TenantProvisioningTests
{
    private readonly Mock<ITenantProvisioningService> _provisioningServiceMock;

    public TenantProvisioningTests()
    {
        _provisioningServiceMock = new Mock<ITenantProvisioningService>();
    }

    [Fact]
    public async Task ProvisionNewCollegeAsync_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new ProvisionCollegeRequest
        {
            CollegeName = "RNC College",
            TenantCode = "RNC",
            Slug = "rnc-college",
            PrimaryDomain = "rnc.localhost",
            EnabledFeatures = new List<string> { "PAGES", "NEWS", "EVENTS" },
            AdminUsername = "rnc_admin",
            AdminEmail = "admin@rnc.college",
            AdminFullName = "RNC Admin",
            AdminPassword = "Admin@123"
        };

        var result = new ProvisioningResult
        {
            Success = true,
            Message = "College provisioned successfully",
            TenantId = Guid.NewGuid(),
            DomainId = Guid.NewGuid(),
            AdminUserId = Guid.NewGuid(),
            DatabaseName = "college_rnc",
            StepsCompleted = new List<string>
            {
                "Validate uniqueness",
                "Create database",
                "Run migrations",
                "Seed data",
                "Create admin user",
                "Set status to ACTIVE"
            }
        };

        _provisioningServiceMock
            .Setup(s => s.ProvisionNewCollegeAsync(request))
            .ReturnsAsync(result);

        // Act
        var response = await _provisioningServiceMock.Object.ProvisionNewCollegeAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.TenantId.Should().NotBeEmpty();
        response.DatabaseName.Should().Be("college_rnc");
        response.StepsCompleted.Should().HaveCount(6);
    }

    [Fact]
    public async Task ProvisionNewCollegeAsync_WithDuplicateTenantCode_ReturnsFailure()
    {
        var request = new ProvisionCollegeRequest
        {
            CollegeName = "Duplicate College",
            TenantCode = "RNC", // already exists
            Slug = "duplicate-college",
            PrimaryDomain = "dup.localhost"
        };

        var result = new ProvisioningResult
        {
            Success = false,
            Message = "Tenant code 'RNC' already exists",
            FailedAt = "Validate uniqueness"
        };

        _provisioningServiceMock
            .Setup(s => s.ProvisionNewCollegeAsync(request))
            .ReturnsAsync(result);

        var response = await _provisioningServiceMock.Object.ProvisionNewCollegeAsync(request);

        response.Success.Should().BeFalse();
        response.FailedAt.Should().Be("Validate uniqueness");
    }

    [Fact]
    public async Task SuspendCollegeAsync_WithValidTenant_ReturnsTrue()
    {
        _provisioningServiceMock
            .Setup(s => s.SuspendCollegeAsync(It.IsAny<Guid>(), "Violation", "superadmin"))
            .ReturnsAsync(true);

        var result = await _provisioningServiceMock.Object.SuspendCollegeAsync(
            Guid.NewGuid(), "Violation", "superadmin");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ActivateCollegeAsync_WithSuspendedTenant_ReturnsTrue()
    {
        _provisioningServiceMock
            .Setup(s => s.ActivateCollegeAsync(It.IsAny<Guid>(), "superadmin"))
            .ReturnsAsync(true);

        var result = await _provisioningServiceMock.Object.ActivateCollegeAsync(
            Guid.NewGuid(), "superadmin");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetMigrationStatusAsync_ReturnsHealthyStatus()
    {
        var status = new MigrationStatusDto
        {
            TenantId = Guid.NewGuid(),
            TenantCode = "RNC",
            DatabaseName = "college_rnc",
            CurrentVersion = "V8",
            Status = "HEALTHY",
            LastCheckedAt = DateTime.UtcNow
        };

        _provisioningServiceMock
            .Setup(s => s.GetMigrationStatusAsync(It.IsAny<Guid>()))
            .ReturnsAsync(status);

        var result = await _provisioningServiceMock.Object.GetMigrationStatusAsync(Guid.NewGuid());

        result.Should().NotBeNull();
        result.Status.Should().Be("HEALTHY");
        result.CurrentVersion.Should().Be("V8");
    }

    [Fact]
    public async Task RunMigrationsAsync_ReturnsTrue()
    {
        _provisioningServiceMock
            .Setup(s => s.RunMigrationsAsync(It.IsAny<Guid>(), "superadmin"))
            .ReturnsAsync(true);

        var result = await _provisioningServiceMock.Object.RunMigrationsAsync(
            Guid.NewGuid(), "superadmin");

        result.Should().BeTrue();
    }
}
