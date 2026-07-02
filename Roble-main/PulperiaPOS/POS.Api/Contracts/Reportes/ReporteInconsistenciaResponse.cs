namespace POS.Api.Contracts.Reportes;

public sealed record ReporteInconsistenciaResponse(
    string Codigo,
    string Severidad,
    string Descripcion,
    int Cantidad,
    string AccionRecomendada);

