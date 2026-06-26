using POS.Api.Contracts.Productos;

namespace POS.Api.Application.Productos;

public interface IProductoService
{
    Task<IReadOnlyCollection<ProductoVentaListItemResponse>> GetProductosAsync(
        ProductoQuery query,
        CancellationToken cancellationToken);

    Task<ProductoVentaLookupResponse?> GetProductoByIdAsync(
        string idProducto,
        CancellationToken cancellationToken);
}
