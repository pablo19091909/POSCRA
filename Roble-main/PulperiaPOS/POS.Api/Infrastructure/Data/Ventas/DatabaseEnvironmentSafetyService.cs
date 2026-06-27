using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using POS.Api.Application.Ventas;
using POS.Api.Configuration;

namespace POS.Api.Infrastructure.Data.Ventas;

public sealed class DatabaseEnvironmentSafetyService : IDatabaseEnvironmentSafetyService
{
    private readonly IDatabaseConnectionFactory connectionFactory;
    private readonly EnvironmentSafetyOptions options;

    public DatabaseEnvironmentSafetyService(
        IDatabaseConnectionFactory connectionFactory,
        IOptions<EnvironmentSafetyOptions> options)
    {
        this.connectionFactory = connectionFactory;
        this.options = options.Value;
    }

    public async Task<bool> CanWriteVentasAsync(CancellationToken cancellationToken)
    {
        if (!options.BlockWritesUnlessDatabaseEnvironmentMatches)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(options.RequiredDatabaseEnvironment))
        {
            return false;
        }

        try
        {
            await using var connection = connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            const string sql = """
                SELECT COUNT_BIG(1)
                FROM dbo.app_environment
                WHERE id = 1
                  AND environment_name = @environment_name
                  AND writes_allowed_for_testing = 1;
                """;

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@environment_name", options.RequiredDatabaseEnvironment.Trim());

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt64(result) == 1;
        }
        catch
        {
            return false;
        }
    }
}
