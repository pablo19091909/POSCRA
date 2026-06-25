using Microsoft.AspNetCore.Authorization;

namespace POS.Api.Infrastructure.Security;

public static class AuthorizationPolicyExtensions
{
    public static AuthorizationPolicyBuilder RequirePermission(this AuthorizationPolicyBuilder builder, string permission)
    {
        return builder.RequireClaim(PermissionClaimTypes.Permission, permission);
    }
}
