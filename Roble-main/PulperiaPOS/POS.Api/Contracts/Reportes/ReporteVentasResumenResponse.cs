namespace POS.Api.Contracts.Reportes;

public sealed record ReporteVentasResumenResponse(
    decimal VentasBrutas,
    decimal MontoReversado,
    decimal VentasNetas,
    int CantidadVentas,
    int CantidadVentasReversadas,
    int CantidadReversas,
    decimal EfectivoVentasBruto,
    decimal ReversasEfectivo,
    decimal EfectivoVentasNeto,
    IReadOnlyCollection<ReporteMetodoPagoTotalResponse> TotalesPorMetodoPago,
    IReadOnlyCollection<ReporteProductoNetoResponse> ProductosNetos);

