using Microsoft.Data.SqlClient;
using POS.Api.Application.Clientes;
using POS.Api.Contracts.Clientes;

namespace POS.Api.Infrastructure.Data.Clientes;

public sealed class ClienteRepository : IClienteRepository
{
    private readonly IDatabaseConnectionFactory connectionFactory;

    public ClienteRepository(IDatabaseConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyCollection<ClienteListItemResponse>> GetClientesAsync(
        ClienteQuery query,
        CancellationToken cancellationToken)
    {
        var clientes = new List<ClienteListItemResponse>();

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var commandText = """
            SELECT idCliente, nombre, saldo, comprobante, fecha_carga_saldo
            FROM cliente
            WHERE (@busqueda IS NULL OR LOWER(nombre) LIKE @busqueda)
            ORDER BY nombre ASC, idCliente ASC
            OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;
            """;

        await using var command = new SqlCommand(commandText, connection);
        command.Parameters.AddWithValue("@busqueda",
            string.IsNullOrWhiteSpace(query.Busqueda)
                ? DBNull.Value
                : $"%{query.Busqueda.Trim().ToLowerInvariant()}%");
        command.Parameters.AddWithValue("@offset", query.Offset);
        command.Parameters.AddWithValue("@limit", query.Limit);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            clientes.Add(new ClienteListItemResponse(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? 0m : reader.GetDecimal(2),
                reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                reader.IsDBNull(4)
                    ? null
                    : new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(4), DateTimeKind.Utc))));
        }

        return clientes;
    }
}
