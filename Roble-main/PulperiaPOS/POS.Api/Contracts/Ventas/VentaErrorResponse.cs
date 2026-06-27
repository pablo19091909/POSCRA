namespace POS.Api.Contracts.Ventas;

public sealed record VentaErrorResponse(
    string TraceId,
    string Message,
    IReadOnlyCollection<string> Errors);
