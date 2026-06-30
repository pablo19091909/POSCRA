using POS.Api.Contracts.Caja;

namespace POS.Api.Application.Caja;

public interface ICajaService
{
    Task<CajaServiceResult<CajaTurnoResponse?>> GetTurnoAbiertoAsync(
        string? cajaCodigo,
        CancellationToken cancellationToken);

    Task<CajaServiceResult<IReadOnlyCollection<MovimientoCajaResponse>>> GetMovimientosAsync(
        long idTurno,
        CancellationToken cancellationToken);

    Task<CajaServiceResult<PreCierreCajaResponse>> GetPreCierreAsync(
        long idTurno,
        CancellationToken cancellationToken);

    Task<CajaServiceResult<CajaTurnoResponse>> AbrirTurnoAsync(
        AbrirCajaTurnoRequest? request,
        int usuarioId,
        string? idempotencyKey,
        CancellationToken cancellationToken);

    Task<CajaServiceResult<MovimientoCajaResponse>> RegistrarIngresoAsync(
        RegistrarIngresoCajaRequest? request,
        int usuarioId,
        string? idempotencyKey,
        CancellationToken cancellationToken);

    Task<CajaServiceResult<MovimientoCajaResponse>> RegistrarRetiroAsync(
        RegistrarRetiroCajaRequest? request,
        int usuarioId,
        string? idempotencyKey,
        CancellationToken cancellationToken);

    Task<CajaServiceResult<CierreCajaResponse>> CerrarTurnoAsync(
        long idTurno,
        CerrarCajaTurnoRequest? request,
        int usuarioId,
        string? idempotencyKeyValue,
        CancellationToken cancellationToken);
}
