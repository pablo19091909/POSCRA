using POS.Api.Contracts.Caja;

namespace POS.Api.Application.Caja;

public interface ICajaIdempotencyService
{
    bool TryParseKey(string? value, out Guid idempotencyKey);

    byte[] ComputeAbrirTurnoRequestHash(AbrirCajaTurnoRequest request, int usuarioId);

    byte[] ComputeIngresoRequestHash(RegistrarIngresoCajaRequest request, int usuarioId);

    byte[] ComputeRetiroRequestHash(RegistrarRetiroCajaRequest request, int usuarioId);

    byte[] ComputeCerrarTurnoRequestHash(
        long idTurno,
        string cajaCodigo,
        CerrarCajaTurnoRequest request,
        int usuarioId);
}
