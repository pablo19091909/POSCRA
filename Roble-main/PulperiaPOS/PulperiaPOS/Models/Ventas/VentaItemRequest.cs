namespace PulperiaPOS.Models.Ventas
{
    public sealed record VentaItemRequest(
        string? ProductoId,
        int Cantidad);
}
