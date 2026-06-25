using POS.Api.Contracts;

namespace POS.Api.Application;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}
