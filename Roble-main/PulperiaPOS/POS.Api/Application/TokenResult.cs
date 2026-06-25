namespace POS.Api.Application;

public sealed record TokenResult(string AccessToken, DateTimeOffset ExpiresAtUtc);
