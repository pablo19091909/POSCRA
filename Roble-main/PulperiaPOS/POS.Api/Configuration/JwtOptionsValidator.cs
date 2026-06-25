namespace POS.Api.Configuration;

public static class JwtOptionsValidator
{
    public static bool HasUsableSigningKey(string? signingKey)
    {
        return !string.IsNullOrWhiteSpace(signingKey)
            && !signingKey.Contains("CONFIGURAR_MEDIANTE_SECRETO", StringComparison.OrdinalIgnoreCase)
            && signingKey.Length >= 32;
    }
}
