using System;
using System.Collections.Generic;

namespace PulperiaPOS.Models.Auth
{
    public sealed class AuthenticatedUser
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public IReadOnlyCollection<string> Permissions { get; set; } = Array.Empty<string>();
    }
}
