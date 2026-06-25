using Microsoft.AspNetCore.Mvc;
using POS.Api.Contracts;

namespace POS.Api.Controllers;

[ApiController]
[Route("api/system")]
public sealed class SystemController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public SystemController(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    [HttpGet("version")]
    public ActionResult<SystemVersionResponse> Version()
    {
        var service = _configuration["Service:Name"] ?? "POS.Api";
        var version = _configuration["Service:Version"] ?? "1.0.0";

        return Ok(new SystemVersionResponse(service, version, _environment.EnvironmentName, DateTimeOffset.UtcNow));
    }
}
