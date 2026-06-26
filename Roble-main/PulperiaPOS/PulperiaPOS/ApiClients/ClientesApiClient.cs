using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PulperiaPOS.Models.Api;
using PulperiaPOS.Models.Clientes;

namespace PulperiaPOS.ApiClients
{
    public sealed class ClientesApiClient : ApiClientBase
    {
        public Task<ApiRequestResult<IReadOnlyCollection<ClienteListItemResponse>>> GetClientesAsync(
            string? busqueda = null,
            bool soloActivos = true,
            int limit = 500,
            int offset = 0,
            CancellationToken cancellationToken = default)
        {
            var relativePath = $"api/clientes?soloActivos={soloActivos.ToString().ToLowerInvariant()}&limit={limit}&offset={offset}";
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                relativePath += $"&busqueda={Uri.EscapeDataString(busqueda)}";
            }

            return SendAsync<IReadOnlyCollection<ClienteListItemResponse>>(
                HttpMethod.Get,
                relativePath,
                requiresAuthentication: true,
                cancellationToken: cancellationToken);
        }
    }
}
