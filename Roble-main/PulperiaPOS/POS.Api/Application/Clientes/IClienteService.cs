using POS.Api.Contracts.Clientes;

namespace POS.Api.Application.Clientes;

public interface IClienteService
{
    Task<IReadOnlyCollection<ClienteListItemResponse>> GetClientesAsync(
        ClienteQuery query,
        CancellationToken cancellationToken);
}
