using POS.Api.Contracts.Reportes;

namespace POS.Api.Application.Reportes;

public sealed class ReporteService : IReporteService
{
    private const int DefaultLimit = 100;
    private const int MaxLimit = 500;
    private readonly IReporteRepository reporteRepository;

    public ReporteService(IReporteRepository reporteRepository)
    {
        this.reporteRepository = reporteRepository;
    }

    public async Task<ReporteVentasResumenResponse> GetVentasResumenAsync(DateTime? desdeUtc, DateTime? hastaUtc, CancellationToken cancellationToken)
    {
        return await reporteRepository.GetVentasResumenAsync(desdeUtc, hastaUtc, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ReporteVentaDetalleResponse>> GetVentasDetalleAsync(DateTime? desdeUtc, DateTime? hastaUtc, int? limit, int? offset, CancellationToken cancellationToken)
    {
        return await reporteRepository.GetVentasDetalleAsync(desdeUtc, hastaUtc, NormalizeLimit(limit), NormalizeOffset(offset), cancellationToken);
    }

    public async Task<IReadOnlyCollection<ReporteReversaResponse>> GetReversasAsync(DateTime? desdeUtc, DateTime? hastaUtc, int? limit, int? offset, CancellationToken cancellationToken)
    {
        return await reporteRepository.GetReversasAsync(desdeUtc, hastaUtc, NormalizeLimit(limit), NormalizeOffset(offset), cancellationToken);
    }

    public async Task<ReporteCajaResumenResponse> GetCajaResumenAsync(DateTime? desdeUtc, DateTime? hastaUtc, CancellationToken cancellationToken)
    {
        return await reporteRepository.GetCajaResumenAsync(desdeUtc, hastaUtc, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ReporteTurnoCajaResponse>> GetTurnosAsync(DateTime? desdeUtc, DateTime? hastaUtc, int? limit, int? offset, CancellationToken cancellationToken)
    {
        return await reporteRepository.GetTurnosAsync(desdeUtc, hastaUtc, NormalizeLimit(limit), NormalizeOffset(offset), cancellationToken);
    }

    public async Task<IReadOnlyCollection<ReporteMovimientoCajaResponse>> GetMovimientosAsync(DateTime? desdeUtc, DateTime? hastaUtc, int? limit, int? offset, CancellationToken cancellationToken)
    {
        return await reporteRepository.GetMovimientosAsync(desdeUtc, hastaUtc, NormalizeLimit(limit), NormalizeOffset(offset), cancellationToken);
    }

    public async Task<IReadOnlyCollection<ReporteInconsistenciaResponse>> GetInconsistenciasAsync(CancellationToken cancellationToken)
    {
        return await reporteRepository.GetInconsistenciasAsync(cancellationToken);
    }

    private static int NormalizeLimit(int? limit)
    {
        return Math.Clamp(limit.GetValueOrDefault(DefaultLimit), 1, MaxLimit);
    }

    private static int NormalizeOffset(int? offset)
    {
        return Math.Max(0, offset.GetValueOrDefault(0));
    }
}

