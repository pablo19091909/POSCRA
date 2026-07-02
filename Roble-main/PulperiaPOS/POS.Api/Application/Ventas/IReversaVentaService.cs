using POS.Api.Contracts.Ventas;

namespace POS.Api.Application.Ventas;

public interface IReversaVentaService
{
    Task<ReversaVentaServiceResult> ReversarVentaEfectivoAsync(
        int factura,
        ReversarVentaRequest? request,
        int usuarioId,
        string traceId,
        CancellationToken cancellationToken);
}
