namespace POS.Api.Contracts.Caja;

public sealed record MovimientoCajaResponse(
    long IdMovimiento,
    long IdTurno,
    string TipoMovimiento,
    string Origen,
    decimal Monto,
    string Moneda,
    DateTimeOffset FechaHoraUtc,
    int UsuarioId,
    int? Factura,
    long? PagoId,
    int? IngresoCajaId,
    int? RetiroCajaId,
    string? Referencia,
    string? Observacion,
    string Estado,
    long? ReversaDeMovimientoId);
