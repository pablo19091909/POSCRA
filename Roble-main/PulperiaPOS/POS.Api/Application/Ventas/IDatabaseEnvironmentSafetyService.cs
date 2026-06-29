namespace POS.Api.Application.Ventas;

public interface IDatabaseEnvironmentSafetyService
{
    Task<bool> CanWriteVentasAsync(CancellationToken cancellationToken);

    Task<bool> CanWriteCajaAsync(CancellationToken cancellationToken);
}
