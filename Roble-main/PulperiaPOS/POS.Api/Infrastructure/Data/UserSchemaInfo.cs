namespace POS.Api.Infrastructure.Data;

public sealed record UserSchemaInfo(
    bool HasModernPasswordHash,
    bool HasPasswordHashVersion,
    bool HasActive,
    bool HasFailedAttempts,
    bool HasLockedUntilUtc,
    bool HasLastLoginUtc,
    bool HasPasswordMigratedUtc);
