namespace POS.Api.Application.Productos;

public sealed record ProductoQuery(
    string? Busqueda,
    string? Codigo,
    bool SoloDisponibles,
    int Limit,
    int Offset);
