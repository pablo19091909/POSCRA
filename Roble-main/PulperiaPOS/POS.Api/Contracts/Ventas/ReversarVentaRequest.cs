namespace POS.Api.Contracts.Ventas;

public sealed record ReversarVentaRequest(
    Guid? IdempotencyKey,
    string? Motivo);
