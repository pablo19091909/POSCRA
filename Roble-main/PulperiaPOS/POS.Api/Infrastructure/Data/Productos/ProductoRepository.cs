using Microsoft.Data.SqlClient;
using POS.Api.Application.Productos;
using POS.Api.Contracts.Productos;

namespace POS.Api.Infrastructure.Data.Productos;

public sealed class ProductoRepository : IProductoRepository
{
    private readonly IDatabaseConnectionFactory connectionFactory;

    public ProductoRepository(IDatabaseConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyCollection<ProductoVentaListItemResponse>> GetProductosAsync(
        ProductoQuery query,
        CancellationToken cancellationToken)
    {
        var productos = new List<ProductoVentaListItemResponse>();

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var commandText = """
            SELECT idProducto, nombre, precio, stock
            FROM inventario
            WHERE (@codigo IS NULL OR idProducto = @codigo)
              AND (@busqueda IS NULL OR idProducto = @busquedaExacta OR LOWER(nombre) LIKE @busqueda)
              AND (@soloDisponibles = 0 OR ISNULL(stock, 0) > 0)
            ORDER BY nombre ASC, idProducto ASC
            OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;
            """;

        await using var command = new SqlCommand(commandText, connection);
        AddQueryParameters(command, query);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var stock = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
            productos.Add(new ProductoVentaListItemResponse(
                reader.GetString(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? 0m : reader.GetDecimal(2),
                stock,
                stock > 0));
        }

        return productos;
    }

    public async Task<ProductoVentaLookupResponse?> GetProductoByIdAsync(
        string idProducto,
        CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var commandText = """
            SELECT idProducto, nombre, precio, stock
            FROM inventario
            WHERE idProducto = @idProducto;
            """;

        await using var command = new SqlCommand(commandText, connection);
        command.Parameters.AddWithValue("@idProducto", idProducto);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var stock = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
        return new ProductoVentaLookupResponse(
            reader.GetString(0),
            reader.GetString(1),
            reader.IsDBNull(2) ? 0m : reader.GetDecimal(2),
            stock,
            stock > 0);
    }

    private static void AddQueryParameters(SqlCommand command, ProductoQuery query)
    {
        var trimmedBusqueda = string.IsNullOrWhiteSpace(query.Busqueda) ? null : query.Busqueda.Trim();
        command.Parameters.AddWithValue("@busqueda",
            trimmedBusqueda is null ? DBNull.Value : $"%{trimmedBusqueda.ToLowerInvariant()}%");
        command.Parameters.AddWithValue("@busquedaExacta",
            trimmedBusqueda is null ? DBNull.Value : trimmedBusqueda);
        command.Parameters.AddWithValue("@codigo",
            string.IsNullOrWhiteSpace(query.Codigo) ? DBNull.Value : query.Codigo.Trim());
        command.Parameters.AddWithValue("@soloDisponibles", query.SoloDisponibles ? 1 : 0);
        command.Parameters.AddWithValue("@offset", query.Offset);
        command.Parameters.AddWithValue("@limit", query.Limit);
    }
}
