using POS.Api.Contracts.Ventas;

namespace POS.Api.Application.Ventas;

public interface IVentaService
{
    Task<VentaServiceResult> CrearVentaAsync(
        CrearVentaRequest? request,
        int usuarioId,
        string traceId,
        CancellationToken cancellationToken);
}
