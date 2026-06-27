namespace PulperiaPOS.Models.Ventas
{
    public sealed record PagoVentaResponse(
        string MetodoPago,
        string Moneda,
        decimal Monto,
        decimal? MontoRecibido,
        decimal? Vuelto,
        decimal? TipoCambioAplicado);
}
