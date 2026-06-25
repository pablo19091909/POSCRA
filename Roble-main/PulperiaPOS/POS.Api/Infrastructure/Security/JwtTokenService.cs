using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using POS.Api.Application;
using POS.Api.Configuration;
using POS.Api.Domain;

namespace POS.Api.Infrastructure.Security;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public bool CanIssueTokens()
    {
        return JwtOptionsValidator.HasUsableSigningKey(_options.SigningKey);
    }

    public TokenResult CreateToken(UserAccount user, IReadOnlyCollection<string> permissions)
    {
        if (!CanIssueTokens())
        {
            throw new InvalidOperationException("JWT signing key is not configured.");
        }

        var expiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(Math.Max(5, _options.AccessTokenMinutes));
        var claims = new List<Claim>
        {
            new(PermissionClaimTypes.UserId, user.Id.ToString()),
            new(PermissionClaimTypes.Username, user.Username),
            new("role", user.Role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        claims.AddRange(permissions.Select(permission => new Claim(PermissionClaimTypes.Permission, permission)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            _options.Issuer,
            _options.Audience,
            claims,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: credentials);

        return new TokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }
}
