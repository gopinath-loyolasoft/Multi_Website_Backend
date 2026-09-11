namespace CollegeWebsitePlatform.Application.Common.Models;

/// <summary>
/// Standardized error codes for consistent API responses.
/// Frontend can use these codes to display appropriate messages.
/// </summary>
public static class ErrorCodes
{
    // Validation
    public const string Validation = "VALIDATION_ERROR";
    public const string InvalidRequest = "INVALID_REQUEST";

    // Authentication & Authorization
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string TokenExpired = "TOKEN_EXPIRED";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";

    // Tenant
    public const string TenantNotFound = "TENANT_NOT_FOUND";
    public const string TenantSuspended = "TENANT_SUSPENDED";
    public const string TenantAccessDenied = "TENANT_ACCESS_DENIED";

    // Feature
    public const string FeatureDisabled = "FEATURE_DISABLED";

    // Resource
    public const string NotFound = "RESOURCE_NOT_FOUND";
    public const string AlreadyExists = "RESOURCE_ALREADY_EXISTS";
    public const string Conflict = "CONFLICT";

    // Database
    public const string DatabaseUnavailable = "DATABASE_UNAVAILABLE";
    public const string DatabaseProvisionFailed = "DATABASE_PROVISION_FAILED";

    // Domain
    public const string DomainNotVerified = "DOMAIN_NOT_VERIFIED";

    // Provisioning
    public const string ProvisioningFailed = "PROVISIONING_FAILED";

    // Upload
    public const string FileTooLarge = "FILE_TOO_LARGE";
    public const string InvalidFileType = "INVALID_FILE_TYPE";
    public const string UploadFailed = "UPLOAD_FAILED";

    // Server
    public const string InternalError = "INTERNAL_ERROR";
}
