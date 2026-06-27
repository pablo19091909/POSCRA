namespace POS.Api.Application.Ventas;

public interface IDatabaseEnvironmentSafetyService
{
    Task<bool> CanWriteVentasAsync(CancellationToken cancellationToken);
}
