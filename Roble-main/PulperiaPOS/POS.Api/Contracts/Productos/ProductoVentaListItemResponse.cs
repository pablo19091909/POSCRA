namespace POS.Api.Contracts.Productos;

public sealed record ProductoVentaListItemResponse(
    string IdProducto,
    string Nombre,
    decimal Precio,
    int StockDisponible,
    bool Disponible);
