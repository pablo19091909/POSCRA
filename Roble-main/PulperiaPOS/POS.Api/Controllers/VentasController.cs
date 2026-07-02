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
public sealed class VentasController : ControllerBase
{
    private readonly IVentaService ventaService;
    private readonly IReversaVentaService reversaVentaService;

    public VentasController(IVentaService ventaService, IReversaVentaService reversaVentaService)
    {
        this.ventaService = ventaService;
        this.reversaVentaService = reversaVentaService;
    }

    [HttpPost]
    [Authorize(Policy = PermissionNames.VentasCrear)]
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

    [HttpPost("{factura:int}/reversas")]
    [Authorize(Policy = PermissionNames.VentasReversar)]
    public async Task<ActionResult<ReversarVentaResponse>> ReversarVenta(
        int factura,
        [FromBody] ReversarVentaRequest? request,
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirst(PermissionClaimTypes.UserId)?.Value;
        if (!int.TryParse(userIdValue, out var usuarioId))
        {
            return Unauthorized(new ErrorResponse(HttpContext.TraceIdentifier, "Token invalido."));
        }

        var result = await reversaVentaService.ReversarVentaEfectivoAsync(
            factura,
            request,
            usuarioId,
            HttpContext.TraceIdentifier,
            cancellationToken);

        return result.Status switch
        {
            ReversaVentaServiceStatus.Disabled => StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new ErrorResponse(HttpContext.TraceIdentifier, "Reversa de venta no disponible.")),
            ReversaVentaServiceStatus.Invalid => BadRequest(
                new VentaErrorResponse(HttpContext.TraceIdentifier, "Solicitud de reversa invalida.", result.Errors)),
            ReversaVentaServiceStatus.Conflict => Conflict(
                new VentaErrorResponse(HttpContext.TraceIdentifier, "Conflicto de idempotencia.", result.Errors)),
            ReversaVentaServiceStatus.InProgress => StatusCode(
                StatusCodes.Status409Conflict,
                new VentaErrorResponse(HttpContext.TraceIdentifier, "Solicitud de reversa en proceso.", result.Errors)),
            ReversaVentaServiceStatus.Success => Ok(result.Response),
            _ => StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new ErrorResponse(HttpContext.TraceIdentifier, "Reversa de venta no disponible."))
        };
    }
}
