namespace POS.Api.Contracts.Ventas;

public sealed record VentaItemResponse(
    string ProductoId,
    string Nombre,
    int Cantidad,
    decimal PrecioUnitario,
    decimal Subtotal);
