namespace POS.Api.Configuration;

public sealed class AuthenticationOptions
{
    public bool Enabled { get; set; }
    public bool EnableLegacyHashUpgrade { get; set; }
    public int BcryptWorkFactor { get; set; } = 12;
}
