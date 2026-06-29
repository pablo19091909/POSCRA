namespace POS.Api.Contracts.Caja;

public sealed record CajaErrorResponse(
    string TraceId,
    string Message,
    IReadOnlyCollection<string> Errors);
