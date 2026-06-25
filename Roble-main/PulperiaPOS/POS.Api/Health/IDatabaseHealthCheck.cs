namespace POS.Api.Health;

public interface IDatabaseHealthCheck
{
    Task<DatabaseHealthCheckResult> CanReadAsync(string traceId, CancellationToken cancellationToken);
}
