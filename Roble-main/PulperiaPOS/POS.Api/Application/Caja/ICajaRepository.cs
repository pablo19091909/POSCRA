namespace POS.Api.Application.Caja;

public interface ICajaRepository
{
    Task<CajaTurnoQuery?> GetTurnoAbiertoAsync(string cajaCodigo, CancellationToken cancellationToken);

    Task<CajaTurnoQuery> AbrirTurnoAsync(
        string cajaCodigo,
        decimal fondoInicial,
        string? observacion,
        int usuarioId,
        Guid idempotencyKey,
        byte[] requestHash,
        CancellationToken cancellationToken);

    Task<MovimientoCajaQuery> RegistrarIngresoAsync(
        string cajaCodigo,
        decimal monto,
        string motivo,
        string? referencia,
        int usuarioId,
        Guid idempotencyKey,
        byte[] requestHash,
        CancellationToken cancellationToken);

    Task<MovimientoCajaQuery> RegistrarRetiroAsync(
        string cajaCodigo,
        decimal monto,
        string motivo,
        string? referencia,
        int usuarioId,
        Guid idempotencyKey,
        byte[] requestHash,
        CancellationToken cancellationToken);

    Task<CajaTurnoQuery?> GetTurnoByIdAsync(long idTurno, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<MovimientoCajaQuery>> GetMovimientosAsync(long idTurno, int limit, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ResumenMovimientoCajaQuery>> GetResumenMovimientosAsync(long idTurno, CancellationToken cancellationToken);

    Task<decimal> CalcularEfectivoEsperadoAsync(long idTurno, CancellationToken cancellationToken);

    Task<CierreTurnoQuery> CerrarTurnoAsync(
        long idTurno,
        decimal efectivoContado,
        string? observacion,
        byte[] rowVersion,
        int usuarioId,
        Guid idempotencyKey,
        byte[] requestHash,
        CancellationToken cancellationToken);
}
