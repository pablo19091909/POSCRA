namespace POS.Api.Contracts.Reportes;

public sealed record ReporteReversaResponse(
    int Factura,
    decimal Monto,
    string Moneda,
    DateTime FechaUtc,
    string Motivo,
    string Estado,
    bool TieneMovimientoCompensatorio,
    bool Consistente);

