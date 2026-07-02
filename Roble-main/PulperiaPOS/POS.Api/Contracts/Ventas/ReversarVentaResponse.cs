namespace POS.Api.Contracts.Ventas;

public sealed record ReversarVentaResponse(
    string Estado,
    int Factura,
    decimal Monto,
    DateTimeOffset FechaHoraUtc,
    string ResultadoIdempotencia);
