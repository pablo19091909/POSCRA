namespace POS.Api.Application.Ventas;

public sealed record VentaTransactionItem(
    string ProductoId,
    string Nombre,
    int Cantidad,
    decimal PrecioUnitario)
{
    public decimal Subtotal => PrecioUnitario * Cantidad;
}
