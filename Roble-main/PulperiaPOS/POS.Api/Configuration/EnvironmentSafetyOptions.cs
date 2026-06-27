namespace POS.Api.Configuration;

public sealed class EnvironmentSafetyOptions
{
    public string RequiredDatabaseEnvironment { get; init; } = "Test";

    public bool BlockWritesUnlessDatabaseEnvironmentMatches { get; init; } = true;
}
