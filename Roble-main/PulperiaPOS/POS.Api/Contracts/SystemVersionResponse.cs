namespace POS.Api.Contracts;

public sealed record SystemVersionResponse(string Service, string Version, string Environment, DateTimeOffset Utc);
