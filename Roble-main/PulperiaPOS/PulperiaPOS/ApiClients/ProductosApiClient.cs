using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PulperiaPOS.Models.Api;
using PulperiaPOS.Models.Productos;

namespace PulperiaPOS.ApiClients
{
    public sealed class ProductosApiClient : ApiClientBase
    {
        public Task<ApiRequestResult<IReadOnlyCollection<ProductoVentaApiResponse>>> GetProductosAsync(
            string? busqueda = null,
            string? codigo = null,
            bool soloDisponibles = false,
            int limit = 100,
            int offset = 0,
            CancellationToken cancellationToken = default)
        {
            var relativePath = $"api/productos?soloDisponibles={soloDisponibles.ToString().ToLowerInvariant()}&limit={limit}&offset={offset}";
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                relativePath += $"&busqueda={Uri.EscapeDataString(busqueda)}";
            }

            if (!string.IsNullOrWhiteSpace(codigo))
            {
                relativePath += $"&codigo={Uri.EscapeDataString(codigo)}";
            }

            return SendAsync<IReadOnlyCollection<ProductoVentaApiResponse>>(
                HttpMethod.Get,
                relativePath,
                requiresAuthentication: true,
                cancellationToken: cancellationToken);
        }

        public async Task<ApiRequestResult<ProductoVentaApiResponse>> BuscarPrimeroParaVentaAsync(
            string textoBusqueda,
            CancellationToken cancellationToken = default)
        {
            var result = await GetProductosAsync(
                busqueda: textoBusqueda,
                limit: 1,
                cancellationToken: cancellationToken);

            if (!result.Success)
            {
                return ApiRequestResult<ProductoVentaApiResponse>.Failed(
                    result.ErrorType,
                    result.Message,
                    result.TraceId);
            }

            return ApiRequestResult<ProductoVentaApiResponse>.Succeeded(result.Data?.FirstOrDefault());
        }
    }
}
