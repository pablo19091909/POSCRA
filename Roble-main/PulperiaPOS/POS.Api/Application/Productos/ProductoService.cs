using POS.Api.Contracts.Productos;
using POS.Api.Infrastructure.Data.Productos;

namespace POS.Api.Application.Productos;

public sealed class ProductoService : IProductoService
{
    private readonly IProductoRepository productoRepository;

    public ProductoService(IProductoRepository productoRepository)
    {
        this.productoRepository = productoRepository;
    }

    public Task<IReadOnlyCollection<ProductoVentaListItemResponse>> GetProductosAsync(
        ProductoQuery query,
        CancellationToken cancellationToken)
    {
        return productoRepository.GetProductosAsync(query, cancellationToken);
    }

    public Task<ProductoVentaLookupResponse?> GetProductoByIdAsync(
        string idProducto,
        CancellationToken cancellationToken)
    {
        return productoRepository.GetProductoByIdAsync(idProducto, cancellationToken);
    }
}
