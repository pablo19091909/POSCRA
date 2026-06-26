namespace POS.Api.Application.Clientes;

public sealed record ClienteQuery(
    bool SoloActivos,
    string? Busqueda,
    int Limit,
    int Offset);
