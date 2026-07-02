namespace POS.Api.Configuration;

public sealed class FeatureFlagsOptions
{
    public bool EnableVentasApiWrite { get; init; } = false;

    public bool EnableCajaApiWrite { get; init; } = false;

    public bool EnableVentasApiEfectivoCajaWrite { get; init; } = false;

    public bool EnableVentasApiReversaCajaWrite { get; init; } = false;
}
