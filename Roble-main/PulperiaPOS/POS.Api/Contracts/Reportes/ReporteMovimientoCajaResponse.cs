namespace POS.Api.Contracts.Reportes;

public sealed record ReporteMovimientoCajaResponse(
    string TipoMovimiento,
    string Estado,
    decimal Monto,
    string Moneda,
    DateTime FechaUtc,
    string Origen,
    int? Factura);

