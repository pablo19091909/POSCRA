using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Application.Ventas;
using POS.Api.Contracts;
using POS.Api.Contracts.Ventas;
using POS.Api.Domain;
using POS.Api.Infrastructure.Security;

namespace POS.Api.Controllers;

[ApiController]
[Route("api/ventas")]
[Authorize(Policy = PermissionNames.VentasCrear)]
public sealed class VentasController : ControllerBase
{
    private readonly IVentaService ventaService;

    public VentasController(IVentaService ventaService)
    {
        this.ventaService = ventaService;
    }

    [HttpPost]
    public async Task<ActionResult<VentaResponse>> CrearVenta(
        [FromBody] CrearVentaRequest? request,
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirst(PermissionClaimTypes.UserId)?.Value;
        if (!int.TryParse(userIdValue, out var usuarioId))
        {
            return Unauthorized(new ErrorResponse(HttpContext.TraceIdentifier, "Token invalido."));
        }

        var result = await ventaService.CrearVentaAsync(request, usuarioId, HttpContext.TraceIdentifier, cancellationToken);

        return result.Status switch
        {
            VentaServiceStatus.Disabled => StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new ErrorResponse(HttpContext.TraceIdentifier, "Venta API no disponible.")),
            VentaServiceStatus.Invalid => BadRequest(
                new VentaErrorResponse(HttpContext.TraceIdentifier, "Solicitud de venta invalida.", result.Errors)),
            VentaServiceStatus.Conflict => Conflict(
                new VentaErrorResponse(HttpContext.TraceIdentifier, "Conflicto de idempotencia.", result.Errors)),
            VentaServiceStatus.InProgress => StatusCode(
                StatusCodes.Status409Conflict,
                new VentaErrorResponse(HttpContext.TraceIdentifier, "Solicitud de venta en proceso.", result.Errors)),
            VentaServiceStatus.Success => Ok(result.Response),
            _ => StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new ErrorResponse(HttpContext.TraceIdentifier, "Venta API no disponible."))
        };
    }
}
