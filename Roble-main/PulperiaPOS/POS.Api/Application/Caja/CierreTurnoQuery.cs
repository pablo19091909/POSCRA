namespace POS.Api.Application.Caja;

public sealed record CierreTurnoQuery(
    CajaTurnoQuery Turno,
    bool CierreDiferenciaCreado,
    IReadOnlyCollection<ResumenMovimientoCajaQuery> Resumen);
