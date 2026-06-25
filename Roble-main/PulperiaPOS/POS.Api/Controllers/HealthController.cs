using Microsoft.AspNetCore.Mvc;
using POS.Api.Contracts;
using POS.Api.Health;
using POS.Api.Infrastructure.Logging;

namespace POS.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    private readonly IDatabaseHealthCheck _databaseHealthCheck;

    public HealthController(IDatabaseHealthCheck databaseHealthCheck)
    {
        _databaseHealthCheck = databaseHealthCheck;
    }

    [HttpGet]
    public ActionResult<HealthResponse> Get()
    {
        return Ok(new HealthResponse("healthy", "POS.Api", DateTimeOffset.UtcNow));
    }

    [HttpGet("database")]
    public async Task<ActionResult<DatabaseHealthResponse>> Database(CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var result = await _databaseHealthCheck.CanReadAsync(traceId, cancellationToken);
        if (result.IsHealthy)
        {
            return Ok(new DatabaseHealthResponse("healthy", "POS.Api", DateTimeOffset.UtcNow, "Conexion validada."));
        }

        return StatusCode(
            StatusCodes.Status503ServiceUnavailable,
            new DatabaseHealthResponse("unhealthy", "POS.Api", DateTimeOffset.UtcNow, SafeErrorMessages.DatabaseUnavailable, traceId));
    }
}
