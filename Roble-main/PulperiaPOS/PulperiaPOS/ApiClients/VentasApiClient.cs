using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PulperiaPOS.Models.Api;
using PulperiaPOS.Models.Ventas;

namespace PulperiaPOS.ApiClients
{
    public sealed class VentasApiClient : ApiClientBase
    {
        public Task<ApiRequestResult<VentaResponse>> CrearVentaAsync(
            CrearVentaRequest request,
            CancellationToken cancellationToken = default)
        {
            return SendAsync<VentaResponse>(
                HttpMethod.Post,
                "api/ventas",
                requiresAuthentication: true,
                body: request,
                cancellationToken: cancellationToken);
        }
    }
}
