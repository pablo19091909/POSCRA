using System;
using System.Collections.Generic;

namespace PulperiaPOS.ApiClients
{
    public sealed class ApiAuthenticationState
    {
        public string AccessToken { get; init; } = string.Empty;
        public DateTimeOffset TokenExpiresAtUtc { get; init; }
        public IReadOnlyCollection<string> Permissions { get; init; } = Array.Empty<string>();
    }
}
