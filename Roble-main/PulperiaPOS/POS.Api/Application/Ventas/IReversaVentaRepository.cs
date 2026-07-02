namespace POS.Api.Application.Ventas;

public interface IReversaVentaRepository
{
    Task<ReversaVentaServiceResult> ReverseVentaEfectivoTransactionalAsync(
        ReversarVentaPreparedCommand command,
        CancellationToken cancellationToken);
}
