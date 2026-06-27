namespace POS.Api.Application.Ventas;

public interface IVentaRepository
{
    Task<VentaIdempotenciaState?> GetIdempotenciaAsync(
        int usuarioId,
        Guid idempotencyKey,
        CancellationToken cancellationToken);

    Task<VentaServiceResult> CreateVentaTransactionalAsync(
        CrearVentaPreparedCommand command,
        CancellationToken cancellationToken);
}
