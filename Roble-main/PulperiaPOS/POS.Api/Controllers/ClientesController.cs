using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Application.Clientes;
using POS.Api.Contracts;
using POS.Api.Contracts.Clientes;
using POS.Api.Domain;

namespace POS.Api.Controllers;

[ApiController]
[Route("api/clientes")]
[Authorize(Policy = PermissionNames.ClientesVer)]
public sealed class ClientesController : ControllerBase
{
    private const int DefaultLimit = 100;
    private const int MaxLimit = 500;
    private readonly IClienteService clienteService;

    public ClientesController(IClienteService clienteService)
    {
        this.clienteService = clienteService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<ClienteListItemResponse>>> GetClientes(
        [FromQuery] bool soloActivos = true,
        [FromQuery] string? busqueda = null,
        [FromQuery] int limit = DefaultLimit,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0 || limit > MaxLimit || offset < 0)
        {
            return BadRequest(new ErrorResponse(HttpContext.TraceIdentifier, "Parametros de paginacion invalidos."));
        }

        if (busqueda?.Length > 100)
        {
            return BadRequest(new ErrorResponse(HttpContext.TraceIdentifier, "Parametro de busqueda invalido."));
        }

        var query = new ClienteQuery(soloActivos, busqueda, limit, offset);
        var clientes = await clienteService.GetClientesAsync(query, cancellationToken);
        return Ok(clientes);
    }
}
