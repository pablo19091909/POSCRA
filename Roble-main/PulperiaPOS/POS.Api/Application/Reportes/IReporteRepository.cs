using POS.Api.Contracts.Reportes;

namespace POS.Api.Application.Reportes;

public interface IReporteRepository
{
    Task<ReporteVentasResumenResponse> GetVentasResumenAsync(DateTime? desdeUtc, DateTime? hastaUtc, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ReporteVentaDetalleResponse>> GetVentasDetalleAsync(DateTime? desdeUtc, DateTime? hastaUtc, int limit, int offset, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ReporteReversaResponse>> GetReversasAsync(DateTime? desdeUtc, DateTime? hastaUtc, int limit, int offset, CancellationToken cancellationToken);

    Task<ReporteCajaResumenResponse> GetCajaResumenAsync(DateTime? desdeUtc, DateTime? hastaUtc, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ReporteTurnoCajaResponse>> GetTurnosAsync(DateTime? desdeUtc, DateTime? hastaUtc, int limit, int offset, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ReporteMovimientoCajaResponse>> GetMovimientosAsync(DateTime? desdeUtc, DateTime? hastaUtc, int limit, int offset, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ReporteInconsistenciaResponse>> GetInconsistenciasAsync(CancellationToken cancellationToken);
}

