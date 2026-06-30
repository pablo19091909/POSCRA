using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PulperiaPOS.Configuration;
using PulperiaPOS.Models.Api;
using PulperiaPOS.Models.Caja;

namespace PulperiaPOS.ApiClients
{
    public sealed class CajaApiClient : ApiClientBase
    {
        public Task<ApiRequestResult<CajaTurnoApiResponse>> GetTurnoAbiertoAsync(
            string cajaCodigo,
            CancellationToken cancellationToken = default)
        {
            if (!FeatureFlags.UseCajaApiRead)
            {
                return Task.FromResult(ApiRequestResult<CajaTurnoApiResponse>.Failed(
                    ApiErrorType.Configuration,
                    "La lectura de Caja API esta deshabilitada."));
            }

            var relativePath = $"api/caja/turnos/abierto?cajaCodigo={Uri.EscapeDataString(cajaCodigo)}";
            return SendAsync<CajaTurnoApiResponse>(
                HttpMethod.Get,
                relativePath,
                requiresAuthentication: true,
                cancellationToken: cancellationToken);
        }

        public Task<ApiRequestResult<CajaTurnoApiResponse>> AbrirTurnoAsync(
            AbrirCajaTurnoApiRequest request,
            string idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            if (!FeatureFlags.UseCajaApiOpenWrite)
            {
                return Task.FromResult(ApiRequestResult<CajaTurnoApiResponse>.Failed(
                    ApiErrorType.Configuration,
                    "La apertura de turno por Caja API esta deshabilitada."));
            }

            var headers = new Dictionary<string, string>
            {
                ["Idempotency-Key"] = idempotencyKey
            };

            return SendAsync<CajaTurnoApiResponse>(
                HttpMethod.Post,
                "api/caja/turnos/abrir",
                requiresAuthentication: true,
                body: request,
                headers: headers,
                cancellationToken: cancellationToken);
        }

        public Task<ApiRequestResult<IReadOnlyCollection<MovimientoCajaApiResponse>>> GetMovimientosAsync(
            long idTurno,
            CancellationToken cancellationToken = default)
        {
            if (!FeatureFlags.UseCajaApiRead)
            {
                return Task.FromResult(ApiRequestResult<IReadOnlyCollection<MovimientoCajaApiResponse>>.Failed(
                    ApiErrorType.Configuration,
                    "La lectura de Caja API esta deshabilitada."));
            }

            return SendAsync<IReadOnlyCollection<MovimientoCajaApiResponse>>(
                HttpMethod.Get,
                $"api/caja/turnos/{idTurno}/movimientos",
                requiresAuthentication: true,
                cancellationToken: cancellationToken);
        }

        public Task<ApiRequestResult<PreCierreCajaApiResponse>> GetPreCierreAsync(
            long idTurno,
            CancellationToken cancellationToken = default)
        {
            if (!FeatureFlags.UseCajaApiRead)
            {
                return Task.FromResult(ApiRequestResult<PreCierreCajaApiResponse>.Failed(
                    ApiErrorType.Configuration,
                    "La lectura de Caja API esta deshabilitada."));
            }

            return SendAsync<PreCierreCajaApiResponse>(
                HttpMethod.Get,
                $"api/caja/turnos/{idTurno}/pre-cierre",
                requiresAuthentication: true,
                cancellationToken: cancellationToken);
        }
    }
}
