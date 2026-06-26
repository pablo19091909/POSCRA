namespace POS.Api.Contracts.Productos;

public sealed record ProductoVentaLookupResponse(
    string IdProducto,
    string Nombre,
    decimal Precio,
    int StockDisponible,
    bool Disponible);
