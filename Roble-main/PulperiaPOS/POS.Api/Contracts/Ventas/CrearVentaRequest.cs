namespace POS.Api.Contracts.Ventas;

public sealed record CrearVentaRequest(
    int ClienteId,
    IReadOnlyCollection<VentaItemRequest>? Items,
    PagoVentaRequest? Pago,
    Guid? IdempotencyKey,
    string? Observaciones,
    decimal? TipoCambioObservado,
    string? ReferenciaPago,
    string? Voucher);
