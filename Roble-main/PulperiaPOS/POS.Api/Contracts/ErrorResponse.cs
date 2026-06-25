namespace POS.Api.Contracts;

public sealed record ErrorResponse(string TraceId, string Message);
