namespace POS.Api.Contracts.Caja;

public sealed record PreCierreCajaResponse(
    long IdTurno,
    string CajaCodigo,
    string Estado,
    DateTimeOffset AperturaUtc,
    decimal EfectivoEsperado,
    string RowVersion,
    IReadOnlyCollection<ResumenMovimientoCajaResponse> Resumen);
