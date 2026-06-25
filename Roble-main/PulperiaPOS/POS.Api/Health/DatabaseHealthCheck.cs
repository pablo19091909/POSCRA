using POS.Api.Infrastructure.Data;

namespace POS.Api.Health;

public sealed class DatabaseHealthCheck : IDatabaseHealthCheck
{
    private readonly IDatabaseConnectionFactory _connectionFactory;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(IDatabaseConnectionFactory connectionFactory, ILogger<DatabaseHealthCheck> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<DatabaseHealthCheckResult> CanReadAsync(string traceId, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 5;
            await command.ExecuteScalarAsync(cancellationToken);

            return new DatabaseHealthCheckResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "Database health check failed. TraceId: {TraceId}. ExceptionType: {ExceptionType}",
                traceId,
                ex.GetType().Name);

            return new DatabaseHealthCheckResult(false);
        }
    }
}
