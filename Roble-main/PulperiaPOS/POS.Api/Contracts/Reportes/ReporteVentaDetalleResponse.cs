namespace POS.Api.Contracts.Reportes;

public sealed record ReporteVentaDetalleResponse(
    int Factura,
    DateTime? Fecha,
    string Estado,
    decimal Total,
    string MetodoPago,
    string Origen,
    bool Reversada,
    DateTime? ReversaUtc,
    decimal ImpactoNeto);

