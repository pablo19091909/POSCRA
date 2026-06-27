namespace PulperiaPOS.Models.Ventas
{
    public sealed record VentaItemResponse(
        string ProductoId,
        string Nombre,
        int Cantidad,
        decimal PrecioUnitario,
        decimal Subtotal);
}
