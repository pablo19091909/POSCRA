namespace POS.Api.Contracts.Reportes;

public sealed record ReporteMetodoPagoTotalResponse(
    string MetodoPago,
    decimal Bruto,
    decimal Reversado,
    decimal Neto,
    int CantidadVentas);

