using System.Security.Cryptography;
using System.Text;

namespace POS.Api.Infrastructure.Security;

public sealed class LegacySha256PasswordVerifier : ILegacyPasswordVerifier
{
    public bool Verify(string password, string legacyHash)
    {
        if (string.IsNullOrWhiteSpace(legacyHash))
        {
            return false;
        }

        var computedHash = ComputeHash(password);
        var computedBytes = Encoding.ASCII.GetBytes(computedHash);
        var storedBytes = Encoding.ASCII.GetBytes(legacyHash.Trim().ToUpperInvariant());

        return computedBytes.Length == storedBytes.Length
            && CryptographicOperations.FixedTimeEquals(computedBytes, storedBytes);
    }

    public string ComputeHash(string password)
    {
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToUpperInvariant();
    }
}
