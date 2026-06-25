namespace POS.Api.Contracts;

public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAtUtc, LoginUserResponse User);
