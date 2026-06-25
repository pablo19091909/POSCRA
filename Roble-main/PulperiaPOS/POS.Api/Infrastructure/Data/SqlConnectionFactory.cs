using Microsoft.Data.SqlClient;
using POS.Api.Configuration;

namespace POS.Api.Infrastructure.Data;

public sealed class SqlConnectionFactory : IDatabaseConnectionFactory
{
    private readonly IConfiguration _configuration;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public SqlConnection CreateConnection()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConfigurationKeys.ApiDatabaseConnectionStringEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = _configuration.GetConnectionString(ConfigurationKeys.PosDatabaseConnectionName);
        }

        if (string.IsNullOrWhiteSpace(connectionString) || IsPlaceholder(connectionString))
        {
            throw new InvalidOperationException("Database connection string is not configured.");
        }

        return new SqlConnection(connectionString);
    }

    private static bool IsPlaceholder(string connectionString)
    {
        var normalizedConnectionString = connectionString.Replace(" ", string.Empty, StringComparison.Ordinal);

        return normalizedConnectionString.Contains("Password=CAMBIAR_ESTE_VALOR", StringComparison.OrdinalIgnoreCase)
            || normalizedConnectionString.Contains("Server=SERVIDOR", StringComparison.OrdinalIgnoreCase)
            || normalizedConnectionString.Contains("Database=BASE", StringComparison.OrdinalIgnoreCase)
            || normalizedConnectionString.Contains("UserId=USUARIO", StringComparison.OrdinalIgnoreCase);
    }
}
