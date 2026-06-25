using POS.Api.Contracts;

namespace POS.Api.Application;

public sealed record AuthResult(bool Succeeded, LoginResponse? Response, AuthFailureReason FailureReason)
{
    public static AuthResult Success(LoginResponse response) => new(true, response, AuthFailureReason.None);
    public static AuthResult Failed(AuthFailureReason reason) => new(false, null, reason);
}
