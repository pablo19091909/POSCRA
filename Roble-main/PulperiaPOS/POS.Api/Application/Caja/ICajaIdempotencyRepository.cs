namespace POS.Api.Application.Caja;

public interface ICajaIdempotencyRepository
{
    Task<CajaIdempotencyState?> FindAsync(
        int usuarioId,
        CajaIdempotencyOperation operacion,
        Guid idempotencyKey,
        CancellationToken cancellationToken);
}
