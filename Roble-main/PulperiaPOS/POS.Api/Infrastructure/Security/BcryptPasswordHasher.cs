using Microsoft.Extensions.Options;
using POS.Api.Configuration;

namespace POS.Api.Infrastructure.Security;

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    private readonly int _workFactor;

    public BcryptPasswordHasher(IOptions<AuthenticationOptions> options)
    {
        _workFactor = Math.Clamp(options.Value.BcryptWorkFactor, 10, 14);
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: _workFactor);
    }

    public bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
