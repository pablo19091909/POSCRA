namespace POS.Api.Contracts.Reportes;

public sealed record ReporteCajaResumenResponse(
    int TurnosAbiertos,
    int TurnosEnCierre,
    int TurnosCerrados,
    decimal FondoInicial,
    decimal Ingresos,
    decimal Retiros,
    decimal VentaEfectivo,
    decimal Reversas,
    decimal CierreDiferencia,
    decimal EfectivoEsperadoCalculado,
    decimal EfectivoContadoCerrado,
    decimal DiferenciaCerrada,
    string Fuente);

