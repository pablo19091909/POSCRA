using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Application.Reportes;
using POS.Api.Contracts.Reportes;
using POS.Api.Domain;

namespace POS.Api.Controllers;

[ApiController]
[Route("api/reportes")]
[Authorize(Policy = PermissionNames.ReportesVer)]
public sealed class ReportesController : ControllerBase
{
    private readonly IReporteService reporteService;

    public ReportesController(IReporteService reporteService)
    {
        this.reporteService = reporteService;
    }

    [HttpGet("ventas/resumen")]
    public async Task<ActionResult<ReporteVentasResumenResponse>> GetVentasResumen(
        [FromQuery] DateTime? desdeUtc,
        [FromQuery] DateTime? hastaUtc,
        CancellationToken cancellationToken)
    {
        return Ok(await reporteService.GetVentasResumenAsync(desdeUtc, hastaUtc, cancellationToken));
    }

    [HttpGet("ventas/detalle")]
    public async Task<ActionResult<IReadOnlyCollection<ReporteVentaDetalleResponse>>> GetVentasDetalle(
        [FromQuery] DateTime? desdeUtc,
        [FromQuery] DateTime? hastaUtc,
        [FromQuery] int? limit,
        [FromQuery] int? offset,
        CancellationToken cancellationToken)
    {
        return Ok(await reporteService.GetVentasDetalleAsync(desdeUtc, hastaUtc, limit, offset, cancellationToken));
    }

    [HttpGet("ventas/reversas")]
    public async Task<ActionResult<IReadOnlyCollection<ReporteReversaResponse>>> GetReversas(
        [FromQuery] DateTime? desdeUtc,
        [FromQuery] DateTime? hastaUtc,
        [FromQuery] int? limit,
        [FromQuery] int? offset,
        CancellationToken cancellationToken)
    {
        return Ok(await reporteService.GetReversasAsync(desdeUtc, hastaUtc, limit, offset, cancellationToken));
    }

    [HttpGet("caja/resumen")]
    public async Task<ActionResult<ReporteCajaResumenResponse>> GetCajaResumen(
        [FromQuery] DateTime? desdeUtc,
        [FromQuery] DateTime? hastaUtc,
        CancellationToken cancellationToken)
    {
        return Ok(await reporteService.GetCajaResumenAsync(desdeUtc, hastaUtc, cancellationToken));
    }

    [HttpGet("caja/turnos")]
    public async Task<ActionResult<IReadOnlyCollection<ReporteTurnoCajaResponse>>> GetTurnos(
        [FromQuery] DateTime? desdeUtc,
        [FromQuery] DateTime? hastaUtc,
        [FromQuery] int? limit,
        [FromQuery] int? offset,
        CancellationToken cancellationToken)
    {
        return Ok(await reporteService.GetTurnosAsync(desdeUtc, hastaUtc, limit, offset, cancellationToken));
    }

    [HttpGet("caja/movimientos")]
    public async Task<ActionResult<IReadOnlyCollection<ReporteMovimientoCajaResponse>>> GetMovimientos(
        [FromQuery] DateTime? desdeUtc,
        [FromQuery] DateTime? hastaUtc,
        [FromQuery] int? limit,
        [FromQuery] int? offset,
        CancellationToken cancellationToken)
    {
        return Ok(await reporteService.GetMovimientosAsync(desdeUtc, hastaUtc, limit, offset, cancellationToken));
    }

    [HttpGet("auditoria/inconsistencias")]
    public async Task<ActionResult<IReadOnlyCollection<ReporteInconsistenciaResponse>>> GetInconsistencias(
        CancellationToken cancellationToken)
    {
        return Ok(await reporteService.GetInconsistenciasAsync(cancellationToken));
    }
}

