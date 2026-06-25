namespace POS.Api.Contracts;

public sealed record DatabaseHealthResponse(string Status, string Service, DateTimeOffset Utc, string Message, string? TraceId = null);
