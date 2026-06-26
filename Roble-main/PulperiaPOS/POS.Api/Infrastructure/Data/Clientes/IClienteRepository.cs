using POS.Api.Application.Clientes;
using POS.Api.Contracts.Clientes;

namespace POS.Api.Infrastructure.Data.Clientes;

public interface IClienteRepository
{
    Task<IReadOnlyCollection<ClienteListItemResponse>> GetClientesAsync(
        ClienteQuery query,
        CancellationToken cancellationToken);
}
