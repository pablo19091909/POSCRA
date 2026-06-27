namespace POS.Api.Contracts.Ventas;

public sealed record VentaItemRequest(
    string? ProductoId,
    int Cantidad);
