using POS.Api.Contracts.Caja;

namespace POS.Api.Application.Caja;

public interface ICajaIdempotencyService
{
    bool TryParseKey(string? value, out Guid idempotencyKey);

    byte[] ComputeIngresoRequestHash(RegistrarIngresoCajaRequest request, int usuarioId);
}
