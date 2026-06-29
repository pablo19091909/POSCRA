namespace POS.Api.Contracts.Caja;

public sealed record CajaTurnoResponse(
    long IdTurno,
    string CajaCodigo,
    string Estado,
    int UsuarioAperturaId,
    int? UsuarioCierreId,
    DateTimeOffset AperturaUtc,
    DateTimeOffset? CierreUtc,
    decimal FondoInicial,
    decimal? EfectivoEsperado,
    decimal? EfectivoContado,
    decimal? Diferencia,
    string? ObservacionApertura,
    string? ObservacionCierre,
    int? CierreCajaId,
    string RowVersion);
