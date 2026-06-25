namespace POS.Api.Configuration;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "POS.Api";
    public string Audience { get; set; } = "PulperiaPOS.WPF";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 45;
}
