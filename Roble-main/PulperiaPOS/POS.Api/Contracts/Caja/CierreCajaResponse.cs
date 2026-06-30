namespace POS.Api.Contracts.Caja;

public sealed record CierreCajaResponse(
    long IdTurno,
    string CajaCodigo,
    string Estado,
    decimal EfectivoEsperado,
    decimal EfectivoContado,
    decimal Diferencia,
    DateTimeOffset CierreUtc,
    bool CierreDiferenciaCreado,
    IReadOnlyCollection<ResumenMovimientoCajaResponse> Resumen);
