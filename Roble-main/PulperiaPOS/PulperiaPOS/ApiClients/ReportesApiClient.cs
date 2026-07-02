using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PulperiaPOS.Models.Api;
using PulperiaPOS.Models.Reportes;

namespace PulperiaPOS.ApiClients
{
    public sealed class ReportesApiClient : ApiClientBase
    {
        public Task<ApiRequestResult<ReporteVentasResumenResponse>> GetVentasResumenAsync(
            DateTime? desdeUtc,
            DateTime? hastaUtc,
            CancellationToken cancellationToken = default)
        {
            return SendAsync<ReporteVentasResumenResponse>(
                HttpMethod.Get,
                $"api/reportes/ventas/resumen{BuildDateQuery(desdeUtc, hastaUtc)}",
                requiresAuthentication: true,
                cancellationToken: cancellationToken);
        }

        public Task<ApiRequestResult<IReadOnlyCollection<ReporteVentaDetalleResponse>>> GetVentasDetalleAsync(
            DateTime? desdeUtc,
            DateTime? hastaUtc,
            int limit = 100,
            int offset = 0,
            CancellationToken cancellationToken = default)
        {
            return SendAsync<IReadOnlyCollection<ReporteVentaDetalleResponse>>(
                HttpMethod.Get,
                $"api/reportes/ventas/detalle{BuildDateQuery(desdeUtc, hastaUtc, limit, offset)}",
                requiresAuthentication: true,
                cancellationToken: cancellationToken);
        }

        public Task<ApiRequestResult<IReadOnlyCollection<ReporteReversaResponse>>> GetReversasAsync(
            DateTime? desdeUtc,
            DateTime? hastaUtc,
            int limit = 100,
            int offset = 0,
            CancellationToken cancellationToken = default)
        {
            return SendAsync<IReadOnlyCollection<ReporteReversaResponse>>(
                HttpMethod.Get,
                $"api/reportes/ventas/reversas{BuildDateQuery(desdeUtc, hastaUtc, limit, offset)}",
                requiresAuthentication: true,
                cancellationToken: cancellationToken);
        }

        public Task<ApiRequestResult<ReporteCajaResumenResponse>> GetCajaResumenAsync(
            DateTime? desdeUtc,
            DateTime? hastaUtc,
            CancellationToken cancellationToken = default)
        {
            return SendAsync<ReporteCajaResumenResponse>(
                HttpMethod.Get,
                $"api/reportes/caja/resumen{BuildDateQuery(desdeUtc, hastaUtc)}",
                requiresAuthentication: true,
                cancellationToken: cancellationToken);
        }

        public Task<ApiRequestResult<IReadOnlyCollection<ReporteTurnoCajaResponse>>> GetTurnosAsync(
            DateTime? desdeUtc,
            DateTime? hastaUtc,
            int limit = 100,
            int offset = 0,
            CancellationToken cancellationToken = default)
        {
            return SendAsync<IReadOnlyCollection<ReporteTurnoCajaResponse>>(
                HttpMethod.Get,
                $"api/reportes/caja/turnos{BuildDateQuery(desdeUtc, hastaUtc, limit, offset)}",
                requiresAuthentication: true,
                cancellationToken: cancellationToken);
        }

        public Task<ApiRequestResult<IReadOnlyCollection<ReporteMovimientoCajaResponse>>> GetMovimientosAsync(
            DateTime? desdeUtc,
            DateTime? hastaUtc,
            int limit = 100,
            int offset = 0,
            CancellationToken cancellationToken = default)
        {
            return SendAsync<IReadOnlyCollection<ReporteMovimientoCajaResponse>>(
                HttpMethod.Get,
                $"api/reportes/caja/movimientos{BuildDateQuery(desdeUtc, hastaUtc, limit, offset)}",
                requiresAuthentication: true,
                cancellationToken: cancellationToken);
        }

        public Task<ApiRequestResult<IReadOnlyCollection<ReporteInconsistenciaResponse>>> GetInconsistenciasAsync(
            CancellationToken cancellationToken = default)
        {
            return SendAsync<IReadOnlyCollection<ReporteInconsistenciaResponse>>(
                HttpMethod.Get,
                "api/reportes/auditoria/inconsistencias",
                requiresAuthentication: true,
                cancellationToken: cancellationToken);
        }

        private static string BuildDateQuery(DateTime? desdeUtc, DateTime? hastaUtc, int? limit = null, int? offset = null)
        {
            var parts = new List<string>();
            if (desdeUtc.HasValue)
            {
                parts.Add("desdeUtc=" + Uri.EscapeDataString(desdeUtc.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)));
            }

            if (hastaUtc.HasValue)
            {
                parts.Add("hastaUtc=" + Uri.EscapeDataString(hastaUtc.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)));
            }

            if (limit.HasValue)
            {
                parts.Add("limit=" + limit.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (offset.HasValue)
            {
                parts.Add("offset=" + offset.Value.ToString(CultureInfo.InvariantCulture));
            }

            return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
        }
    }
}
