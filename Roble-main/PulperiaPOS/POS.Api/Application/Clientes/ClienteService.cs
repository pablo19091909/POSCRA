using POS.Api.Contracts.Clientes;
using POS.Api.Infrastructure.Data.Clientes;

namespace POS.Api.Application.Clientes;

public sealed class ClienteService : IClienteService
{
    private readonly IClienteRepository clienteRepository;

    public ClienteService(IClienteRepository clienteRepository)
    {
        this.clienteRepository = clienteRepository;
    }

    public Task<IReadOnlyCollection<ClienteListItemResponse>> GetClientesAsync(
        ClienteQuery query,
        CancellationToken cancellationToken)
    {
        return clienteRepository.GetClientesAsync(query, cancellationToken);
    }
}
