namespace CollegeWebsitePlatform.Domain.Constants;

/// <summary>
/// Centralized constants for tenant and provisioning status strings.
/// Use these instead of magic string literals throughout the codebase.
/// </summary>
public static class TenantStatus
{
    public const string Active       = "ACTIVE";
    public const string Suspended    = "SUSPENDED";
    public const string Provisioning = "PROVISIONING";
    public const string Deactivated  = "DEACTIVATED";
}

public static class ProvisioningStatus
{
    public const string Pending   = "PENDING";
    public const string Running   = "RUNNING";
    public const string Completed = "COMPLETED";
    public const string Failed    = "FAILED";
}

public static class MigrationStatus
{
    public const string Healthy   = "HEALTHY";
    public const string OutOfDate = "OUT_OF_DATE";
    public const string Failed    = "FAILED";
    public const string Unknown   = "UNKNOWN";
}

public static class DomainType
{
    public const string Primary   = "PRIMARY";
    public const string Secondary = "SECONDARY";
    public const string Custom    = "CUSTOM";
}

public static class UserRole
{
    public const string SuperAdmin   = "SuperAdmin";
    public const string CollegeAdmin = "CollegeAdmin";
    public const string Editor       = "Editor";
}
