namespace POS.Api.Application.Caja;

public sealed record CajaTurnoQuery(
    long IdTurno,
    string CajaCodigo,
    string Estado,
    int UsuarioAperturaId,
    int? UsuarioCierreId,
    DateTime AperturaUtc,
    DateTime? CierreUtc,
    decimal FondoInicial,
    decimal? EfectivoEsperado,
    decimal? EfectivoContado,
    decimal? Diferencia,
    string? ObservacionApertura,
    string? ObservacionCierre,
    int? CierreCajaId,
    byte[] RowVersion);
