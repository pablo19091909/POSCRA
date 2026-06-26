using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Application.Productos;
using POS.Api.Contracts;
using POS.Api.Contracts.Productos;
using POS.Api.Domain;

namespace POS.Api.Controllers;

[ApiController]
[Route("api/productos")]
[Authorize(Policy = PermissionNames.InventarioVer)]
public sealed class ProductosController : ControllerBase
{
    private const int DefaultLimit = 100;
    private const int MaxLimit = 500;
    private readonly IProductoService productoService;

    public ProductosController(IProductoService productoService)
    {
        this.productoService = productoService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<ProductoVentaListItemResponse>>> GetProductos(
        [FromQuery] string? busqueda = null,
        [FromQuery] string? codigo = null,
        [FromQuery] bool soloDisponibles = false,
        [FromQuery] int limit = DefaultLimit,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0 || limit > MaxLimit || offset < 0)
        {
            return BadRequest(new ErrorResponse(HttpContext.TraceIdentifier, "Parametros de paginacion invalidos."));
        }

        if (busqueda?.Length > 100 || codigo?.Length > 100)
        {
            return BadRequest(new ErrorResponse(HttpContext.TraceIdentifier, "Parametro de busqueda invalido."));
        }

        var query = new ProductoQuery(busqueda, codigo, soloDisponibles, limit, offset);
        var productos = await productoService.GetProductosAsync(query, cancellationToken);
        return Ok(productos);
    }

    [HttpGet("{idProducto}")]
    public async Task<ActionResult<ProductoVentaLookupResponse>> GetProductoById(
        string idProducto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idProducto) || idProducto.Length > 100)
        {
            return BadRequest(new ErrorResponse(HttpContext.TraceIdentifier, "Parametro de producto invalido."));
        }

        var producto = await productoService.GetProductoByIdAsync(idProducto, cancellationToken);
        if (producto is null)
        {
            return NotFound(new ErrorResponse(HttpContext.TraceIdentifier, "Producto no encontrado."));
        }

        return Ok(producto);
    }
}
