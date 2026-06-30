using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Application.Caja;
using POS.Api.Contracts;
using POS.Api.Contracts.Caja;
using POS.Api.Domain;
using POS.Api.Infrastructure.Security;

namespace POS.Api.Controllers;

[ApiController]
[Route("api/caja")]
[Authorize]
public sealed class CajaController : ControllerBase
{
    private readonly ICajaService cajaService;

    public CajaController(ICajaService cajaService)
    {
        this.cajaService = cajaService;
    }

    [HttpGet("turnos/abierto")]
    [Authorize(Policy = PermissionNames.CajaVer)]
    public async Task<ActionResult<CajaTurnoResponse?>> GetTurnoAbierto(
        [FromQuery] string? cajaCodigo,
        CancellationToken cancellationToken)
    {
        var result = await cajaService.GetTurnoAbiertoAsync(cajaCodigo, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("turnos/abrir")]
    [Authorize(Policy = PermissionNames.CajaAbrir)]
    public async Task<ActionResult<CajaTurnoResponse>> AbrirTurno(
        [FromBody] AbrirCajaTurnoRequest? request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized(new ErrorResponse(HttpContext.TraceIdentifier, "Token invalido."));
        }

        var result = await cajaService.AbrirTurnoAsync(
            request,
            usuarioId,
            Request.Headers["Idempotency-Key"].FirstOrDefault(),
            cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("ingresos")]
    [Authorize(Policy = PermissionNames.CajaIngresar)]
    public async Task<ActionResult<MovimientoCajaResponse>> RegistrarIngreso(
        [FromBody] RegistrarIngresoCajaRequest? request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized(new ErrorResponse(HttpContext.TraceIdentifier, "Token invalido."));
        }

        var result = await cajaService.RegistrarIngresoAsync(
            request,
            usuarioId,
            Request.Headers["Idempotency-Key"].FirstOrDefault(),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("retiros")]
    [Authorize(Policy = PermissionNames.CajaRetirar)]
    public async Task<ActionResult<MovimientoCajaResponse>> RegistrarRetiro(
        [FromBody] RegistrarRetiroCajaRequest? request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized(new ErrorResponse(HttpContext.TraceIdentifier, "Token invalido."));
        }

        var result = await cajaService.RegistrarRetiroAsync(
            request,
            usuarioId,
            Request.Headers["Idempotency-Key"].FirstOrDefault(),
            cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("turnos/{idTurno:long}/pre-cierre")]
    [Authorize(Policy = PermissionNames.CajaVer)]
    public async Task<ActionResult<PreCierreCajaResponse>> GetPreCierre(
        long idTurno,
        CancellationToken cancellationToken)
    {
        var result = await cajaService.GetPreCierreAsync(idTurno, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("turnos/{idTurno:long}/cerrar")]
    [Authorize(Policy = PermissionNames.CajaCerrar)]
    public async Task<ActionResult<CierreCajaResponse>> CerrarTurno(
        long idTurno,
        [FromBody] CerrarCajaTurnoRequest? request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized(new ErrorResponse(HttpContext.TraceIdentifier, "Token invalido."));
        }

        var result = await cajaService.CerrarTurnoAsync(
            idTurno,
            request,
            usuarioId,
            Request.Headers["Idempotency-Key"].FirstOrDefault(),
            cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("turnos/{idTurno:long}/movimientos")]
    [Authorize(Policy = PermissionNames.CajaVer)]
    public async Task<ActionResult<IReadOnlyCollection<MovimientoCajaResponse>>> GetMovimientos(
        long idTurno,
        CancellationToken cancellationToken)
    {
        var result = await cajaService.GetMovimientosAsync(idTurno, cancellationToken);
        return ToActionResult(result);
    }

    private bool TryGetUsuarioId(out int usuarioId)
    {
        var userIdValue = User.FindFirst(PermissionClaimTypes.UserId)?.Value;
        return int.TryParse(userIdValue, out usuarioId);
    }

    private ActionResult<T> ToActionResult<T>(CajaServiceResult<T> result)
    {
        return result.Status switch
        {
            CajaServiceStatus.Disabled => StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new ErrorResponse(HttpContext.TraceIdentifier, "Caja API no disponible.")),
            CajaServiceStatus.Invalid => BadRequest(
                new CajaErrorResponse(HttpContext.TraceIdentifier, "Solicitud de caja invalida.", result.Errors)),
            CajaServiceStatus.NotFound => NotFound(
                new CajaErrorResponse(HttpContext.TraceIdentifier, "Recurso de caja no encontrado.", result.Errors)),
            CajaServiceStatus.Conflict => Conflict(
                new CajaErrorResponse(HttpContext.TraceIdentifier, "Conflicto de caja.", result.Errors)),
            CajaServiceStatus.Success => Ok(result.Response),
            _ => StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new ErrorResponse(HttpContext.TraceIdentifier, "Caja API no disponible."))
        };
    }
}
