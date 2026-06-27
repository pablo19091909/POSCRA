namespace PulperiaPOS.Models.Ventas
{
    public sealed record PagoVentaRequest(
        string? MetodoPago,
        decimal? MontoRecibido,
        string? Referencia,
        string? Voucher,
        string? Moneda,
        decimal? TipoCambioObservado);
}
