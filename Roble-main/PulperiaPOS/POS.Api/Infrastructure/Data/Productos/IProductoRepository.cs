using POS.Api.Application.Productos;
using POS.Api.Contracts.Productos;

namespace POS.Api.Infrastructure.Data.Productos;

public interface IProductoRepository
{
    Task<IReadOnlyCollection<ProductoVentaListItemResponse>> GetProductosAsync(
        ProductoQuery query,
        CancellationToken cancellationToken);

    Task<ProductoVentaLookupResponse?> GetProductoByIdAsync(
        string idProducto,
        CancellationToken cancellationToken);
}
