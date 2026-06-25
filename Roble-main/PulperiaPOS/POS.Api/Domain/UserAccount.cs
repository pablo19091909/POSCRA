namespace POS.Api.Domain;

public sealed record UserAccount(
    int Id,
    string Username,
    string LegacyPasswordHash,
    string? ModernPasswordHash,
    string? PasswordHashVersion,
    string Role,
    bool? Active,
    DateTimeOffset? LockedUntilUtc);
