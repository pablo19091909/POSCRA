namespace POS.Api.Contracts.Reportes;

public sealed record ReporteTurnoCajaResponse(
    string CajaCodigo,
    string Estado,
    DateTime AperturaUtc,
    DateTime? CierreUtc,
    decimal FondoInicial,
    decimal EfectivoEsperado,
    decimal? EfectivoContado,
    decimal? Diferencia,
    string Fuente);

