using System;
using System.Collections.Generic;
using System.Linq;

namespace PulperiaPOS
{
    public static class UserSession
    {
        public static int IdUsuario { get; set; } = 0;
        public static string NombreUsuario { get; set; } = string.Empty;
        public static string RolUsuario { get; set; } = string.Empty;
        public static bool IsApiAuthenticated { get; set; } = false;
        public static string AccessToken { get; set; } = string.Empty;
        public static DateTimeOffset? TokenExpiresAtUtc { get; set; }
        public static IReadOnlyCollection<string> Permissions { get; set; } = Array.Empty<string>();

        public static bool HasPermission(string permission)
        {
            if (string.IsNullOrWhiteSpace(permission))
            {
                return false;
            }

            return Permissions.Any(current =>
                string.Equals(current, permission, StringComparison.Ordinal));
        }

        public static void Clear()
        {
            IdUsuario = 0;
            NombreUsuario = string.Empty;
            RolUsuario = string.Empty;
            IsApiAuthenticated = false;
            AccessToken = string.Empty;
            TokenExpiresAtUtc = null;
            Permissions = Array.Empty<string>();
        }
    }
}
