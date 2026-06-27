namespace POS.Api.Application.Ventas;

public sealed record VentaPaymentCalculation(
    string MetodoPago,
    string Moneda,
    decimal MontoVenta,
    decimal? MontoRecibido,
    decimal? Vuelto,
    decimal? TipoCambioAplicado,
    string? Referencia,
    string? Voucher,
    string? NumeroVoucherVenta,
    string? NumeroComprobanteVenta,
    decimal MontoPagadoVenta);
