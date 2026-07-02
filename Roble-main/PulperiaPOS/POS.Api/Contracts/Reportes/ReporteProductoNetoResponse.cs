namespace POS.Api.Contracts.Reportes;

public sealed record ReporteProductoNetoResponse(
    string Producto,
    decimal CantidadBruta,
    decimal CantidadRestaurada,
    decimal CantidadNeta,
    decimal VentaNeta);

