namespace POS.Api.Application.Caja;

public sealed record MovimientoCajaQuery(
    long IdMovimiento,
    long IdTurno,
    string TipoMovimiento,
    string Origen,
    decimal Monto,
    string Moneda,
    DateTime FechaHoraUtc,
    int UsuarioId,
    int? Factura,
    long? PagoId,
    int? IngresoCajaId,
    int? RetiroCajaId,
    string? Referencia,
    string? Observacion,
    string Estado,
    long? ReversaDeMovimientoId);
