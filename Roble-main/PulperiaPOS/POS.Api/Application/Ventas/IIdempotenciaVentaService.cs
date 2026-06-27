using POS.Api.Contracts.Ventas;

namespace POS.Api.Application.Ventas;

public interface IIdempotenciaVentaService
{
    byte[] ComputeRequestHash(CrearVentaRequest request);

    Task<VentaIdempotenciaState?> FindAsync(
        int usuarioId,
        Guid idempotencyKey,
        CancellationToken cancellationToken);
}
