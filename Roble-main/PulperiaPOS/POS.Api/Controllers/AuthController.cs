using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using POS.Api.Application;
using POS.Api.Contracts;
using POS.Api.Infrastructure.Logging;
using POS.Api.Infrastructure.Security;

namespace POS.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private const string InvalidCredentialsMessage = "Credenciales invalidas.";
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [EnableRateLimiting("AuthLogin")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest? request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ErrorResponse(HttpContext.TraceIdentifier, "Solicitud de autenticacion invalida."));
        }

        var result = await _authService.LoginAsync(request, cancellationToken);
        if (result.Succeeded && result.Response is not null)
        {
            return Ok(result.Response);
        }

        if (result.FailureReason == AuthFailureReason.AuthConfigurationUnavailable)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ErrorResponse(HttpContext.TraceIdentifier, SafeErrorMessages.AuthUnavailable));
        }

        return Unauthorized(new ErrorResponse(HttpContext.TraceIdentifier, InvalidCredentialsMessage));
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<LoginUserResponse> Me()
    {
        var userIdValue = User.FindFirst(PermissionClaimTypes.UserId)?.Value;
        var username = User.FindFirst(PermissionClaimTypes.Username)?.Value;
        var role = User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value;

        if (!int.TryParse(userIdValue, out var userId) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(role))
        {
            return Unauthorized(new ErrorResponse(HttpContext.TraceIdentifier, "Token invalido."));
        }

        var permissions = User.FindAll(PermissionClaimTypes.Permission)
            .Select(claim => claim.Value)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(permission => permission, StringComparer.Ordinal)
            .ToArray();

        return Ok(new LoginUserResponse(userId, username, role, permissions));
    }

    [Authorize]
    [HttpGet("permission-test/{permission}")]
    public IActionResult PermissionTest(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            return BadRequest(new ErrorResponse(HttpContext.TraceIdentifier, "Permiso invalido."));
        }

        var hasPermission = User.HasClaim(PermissionClaimTypes.Permission, permission);
        if (!hasPermission)
        {
            return Forbid();
        }

        return Ok(new { status = "allowed", permission });
    }
}
