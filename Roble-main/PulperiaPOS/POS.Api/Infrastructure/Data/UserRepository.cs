using Microsoft.Data.SqlClient;
using POS.Api.Domain;

namespace POS.Api.Infrastructure.Data;

public sealed class UserRepository : IUserRepository
{
    private readonly IDatabaseConnectionFactory _connectionFactory;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(IDatabaseConnectionFactory connectionFactory, ILogger<UserRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<UserAccount?> FindByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var schema = await GetUserSchemaInfoAsync(connection, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = BuildUserQuery(schema);
        command.Parameters.Add(new SqlParameter("@username", System.Data.SqlDbType.NVarChar, 100) { Value = username });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new UserAccount(
            reader.GetInt32(reader.GetOrdinal("idUsuario")),
            reader.GetString(reader.GetOrdinal("nombre")),
            reader.GetString(reader.GetOrdinal("contrasena")),
            schema.HasModernPasswordHash ? ReadNullableString(reader, "password_hash_v2") : null,
            schema.HasPasswordHashVersion ? ReadNullableString(reader, "password_hash_version") : null,
            ReadNullableString(reader, "rol") ?? string.Empty,
            schema.HasActive ? reader.GetBoolean(reader.GetOrdinal("activo")) : null,
            schema.HasLockedUntilUtc ? ReadNullableDateTimeOffset(reader, "bloqueado_hasta_utc") : null);
    }

    public async Task<bool> TryUpgradeLegacyPasswordHashAsync(int userId, string modernPasswordHash, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var schema = await GetUserSchemaInfoAsync(connection, cancellationToken);
        if (!schema.HasModernPasswordHash || !schema.HasPasswordHashVersion || !schema.HasPasswordMigratedUtc)
        {
            _logger.LogInformation("Legacy password upgrade skipped because auth migration columns are missing.");
            return false;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE usuario
            SET password_hash_v2 = @passwordHash,
                password_hash_version = @hashVersion,
                password_migrada_utc = SYSUTCDATETIME()
            WHERE idUsuario = @userId
              AND password_hash_v2 IS NULL;
            """;
        command.Parameters.Add(new SqlParameter("@passwordHash", System.Data.SqlDbType.NVarChar, 255) { Value = modernPasswordHash });
        command.Parameters.Add(new SqlParameter("@hashVersion", System.Data.SqlDbType.NVarChar, 50) { Value = "bcrypt" });
        command.Parameters.Add(new SqlParameter("@userId", System.Data.SqlDbType.Int) { Value = userId });

        var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
        return affectedRows == 1;
    }

    private static string BuildUserQuery(UserSchemaInfo schema)
    {
        var columns = new List<string>
        {
            "idUsuario",
            "nombre",
            "contrasena",
            "rol"
        };

        if (schema.HasModernPasswordHash)
        {
            columns.Add("password_hash_v2");
        }

        if (schema.HasPasswordHashVersion)
        {
            columns.Add("password_hash_version");
        }

        if (schema.HasActive)
        {
            columns.Add("activo");
        }

        if (schema.HasLockedUntilUtc)
        {
            columns.Add("bloqueado_hasta_utc");
        }

        return $"SELECT TOP (1) {string.Join(", ", columns)} FROM usuario WHERE nombre = @username ORDER BY idUsuario;";
    }

    private static async Task<UserSchemaInfo> GetUserSchemaInfoAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                CAST(CASE WHEN COL_LENGTH('dbo.usuario', 'password_hash_v2') IS NULL THEN 0 ELSE 1 END AS bit) AS HasModernPasswordHash,
                CAST(CASE WHEN COL_LENGTH('dbo.usuario', 'password_hash_version') IS NULL THEN 0 ELSE 1 END AS bit) AS HasPasswordHashVersion,
                CAST(CASE WHEN COL_LENGTH('dbo.usuario', 'activo') IS NULL THEN 0 ELSE 1 END AS bit) AS HasActive,
                CAST(CASE WHEN COL_LENGTH('dbo.usuario', 'intentos_fallidos') IS NULL THEN 0 ELSE 1 END AS bit) AS HasFailedAttempts,
                CAST(CASE WHEN COL_LENGTH('dbo.usuario', 'bloqueado_hasta_utc') IS NULL THEN 0 ELSE 1 END AS bit) AS HasLockedUntilUtc,
                CAST(CASE WHEN COL_LENGTH('dbo.usuario', 'ultimo_login_utc') IS NULL THEN 0 ELSE 1 END AS bit) AS HasLastLoginUtc,
                CAST(CASE WHEN COL_LENGTH('dbo.usuario', 'password_migrada_utc') IS NULL THEN 0 ELSE 1 END AS bit) AS HasPasswordMigratedUtc;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);

        return new UserSchemaInfo(
            reader.GetBoolean(0),
            reader.GetBoolean(1),
            reader.GetBoolean(2),
            reader.GetBoolean(3),
            reader.GetBoolean(4),
            reader.GetBoolean(5),
            reader.GetBoolean(6));
    }

    private static string? ReadNullableString(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static DateTimeOffset? ReadNullableDateTimeOffset(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        return new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(ordinal), DateTimeKind.Utc));
    }
}
