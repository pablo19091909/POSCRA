using System;

namespace PulperiaPOS.Models.Auth
{
    public sealed class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAtUtc { get; set; }
        public AuthenticatedUser? User { get; set; }
    }
}
