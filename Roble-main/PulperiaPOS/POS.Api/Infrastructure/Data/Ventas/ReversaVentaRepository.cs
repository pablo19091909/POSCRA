using System.Data;
using Microsoft.Data.SqlClient;
using POS.Api.Application.Ventas;
using POS.Api.Contracts.Ventas;

namespace POS.Api.Infrastructure.Data.Ventas;

public sealed class ReversaVentaRepository : IReversaVentaRepository
{
    private const string CajaCodigoVentaEfectivo = "CAJA_PRINCIPAL_TEST";
    private const string EstadoTurnoAbierto = "Abierto";
    private const string EstadoConfirmado = "Confirmado";
    private const string TipoVentaEfectivo = "VentaEfectivo";
    private const string TipoReversa = "Reversa";
    private const string OrigenApi = "POS.Api";
    private const string MonedaColones = "CRC";

    private readonly IDatabaseConnectionFactory connectionFactory;

    public ReversaVentaRepository(IDatabaseConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<ReversaVentaServiceResult> ReverseVentaEfectivoTransactionalAsync(
        ReversarVentaPreparedCommand command,
        CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        try
        {
            var idempotencia = await GetIdempotenciaForUpdateAsync(connection, (SqlTransaction)transaction, command, cancellationToken);
            if (idempotencia is not null)
            {
                var existing = await ResolveExistingIdempotenciaAsync(
                    connection,
                    (SqlTransaction)transaction,
                    command,
                    idempotencia,
                    cancellationToken);

                if (existing is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                    return existing;
                }
            }

            var idIdempotencia = idempotencia?.IdIdempotencia
                ?? await InsertIdempotenciaAsync(connection, (SqlTransaction)transaction, command, cancellationToken);

            await EnsureUsuarioActivoAsync(connection, (SqlTransaction)transaction, command.UsuarioId, cancellationToken);

            var venta = await LoadEligibleVentaForUpdateAsync(
                connection,
                (SqlTransaction)transaction,
                command.Factura,
                cancellationToken);

            if (venta is null)
            {
                throw new VentaBusinessException(VentaServiceStatus.Conflict, "Venta no elegible para reversa.");
            }

            if (await VentaYaReversadaAsync(connection, (SqlTransaction)transaction, command.Factura, cancellationToken))
            {
                throw new VentaBusinessException(VentaServiceStatus.Conflict, "La venta ya fue reversada.");
            }

            var efectivoDisponible = await CalcularEfectivoDisponibleAsync(
                connection,
                (SqlTransaction)transaction,
                venta.IdTurno,
                cancellationToken);

            if (efectivoDisponible < venta.Monto)
            {
                throw new VentaBusinessException(VentaServiceStatus.Conflict, "Efectivo insuficiente para reversar la venta.");
            }

            var items = await LoadDetalleForUpdateAsync(connection, (SqlTransaction)transaction, command.Factura, cancellationToken);
            if (items.Count == 0)
            {
                throw new VentaBusinessException(VentaServiceStatus.Conflict, "Venta no elegible para reversa.");
            }

            var fechaHoraUtc = DateTime.UtcNow;
            foreach (var item in items)
            {
                await RestoreStockAsync(connection, (SqlTransaction)transaction, item, cancellationToken);
            }

            var idMovimientoCompensatorio = await InsertMovimientoReversaAsync(
                connection,
                (SqlTransaction)transaction,
                venta,
                command.UsuarioId,
                command.Request.Motivo!.Trim(),
                fechaHoraUtc,
                cancellationToken);

            await InsertVentaReversaAsync(
                connection,
                (SqlTransaction)transaction,
                command,
                venta,
                idIdempotencia,
                idMovimientoCompensatorio,
                fechaHoraUtc,
                cancellationToken);

            await InsertAuditoriaAsync(
                connection,
                (SqlTransaction)transaction,
                command,
                idIdempotencia,
                fechaHoraUtc,
                cancellationToken);

            await CompleteIdempotenciaAsync(
                connection,
                (SqlTransaction)transaction,
                idIdempotencia,
                command.Factura,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return ReversaVentaServiceResult.Success(new ReversarVentaResponse(
                "Confirmada",
                command.Factura,
                venta.Monto,
                new DateTimeOffset(DateTime.SpecifyKind(fechaHoraUtc, DateTimeKind.Utc)),
                "Nueva"));
        }
        catch (VentaBusinessException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return ex.Status == VentaServiceStatus.Conflict
                ? ReversaVentaServiceResult.Conflict(ex.SafeMessage)
                : ReversaVentaServiceResult.Invalid([ex.SafeMessage]);
        }
        catch (SqlException ex) when (IsUniqueConstraintViolation(ex))
        {
            await transaction.RollbackAsync(cancellationToken);
            return ReversaVentaServiceResult.Conflict("La venta ya fue reversada.");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            return ReversaVentaServiceResult.Invalid(["No fue posible completar la reversa."]);
        }
    }

    private static async Task<VentaIdempotenciaState?> GetIdempotenciaForUpdateAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        ReversarVentaPreparedCommand command,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT idIdempotencia,
                   idempotency_key,
                   usuario_id,
                   request_hash,
                   estado,
                   factura,
                   response_hash,
                   error_code,
                   creado_utc,
                   actualizado_utc,
                   expira_utc
            FROM dbo.venta_idempotencia WITH (UPDLOCK, HOLDLOCK)
            WHERE usuario_id = @usuario_id
              AND idempotency_key = @idempotency_key;
            """;

        await using var sqlCommand = new SqlCommand(sql, connection, transaction);
        sqlCommand.Parameters.Add("@usuario_id", SqlDbType.Int).Value = command.UsuarioId;
        sqlCommand.Parameters.Add("@idempotency_key", SqlDbType.UniqueIdentifier).Value = command.Request.IdempotencyKey!.Value;

        await using var reader = await sqlCommand.ExecuteReaderAsync(cancellationToken);
        return await ReadIdempotenciaAsync(reader, cancellationToken);
    }

    private static async Task<ReversaVentaServiceResult?> ResolveExistingIdempotenciaAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        ReversarVentaPreparedCommand command,
        VentaIdempotenciaState state,
        CancellationToken cancellationToken)
    {
        if (!state.RequestHash.SequenceEqual(command.RequestHash))
        {
            return ReversaVentaServiceResult.Conflict("Idempotency key ya fue usada con una solicitud distinta.");
        }

        if (string.Equals(state.Estado, "EnProceso", StringComparison.Ordinal))
        {
            return ReversaVentaServiceResult.InProgress();
        }

        if (string.Equals(state.Estado, "Completada", StringComparison.Ordinal))
        {
            var response = await LoadCompletedReversaResponseAsync(connection, transaction, state.IdIdempotencia, cancellationToken);
            return response is null
                ? ReversaVentaServiceResult.Conflict("No fue posible recuperar la reversa completada.")
                : ReversaVentaServiceResult.Success(response with { ResultadoIdempotencia = "Repetida" });
        }

        if (string.Equals(state.Estado, "Fallida", StringComparison.Ordinal) && state.Factura is null)
        {
            await ResetFailedIdempotenciaAsync(connection, transaction, state.IdIdempotencia, command.RequestHash, cancellationToken);
            return null;
        }

        return ReversaVentaServiceResult.Conflict("Estado de idempotencia no permite continuar.");
    }

    private static async Task<long> InsertIdempotenciaAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        ReversarVentaPreparedCommand command,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.venta_idempotencia
                (idempotency_key, usuario_id, request_hash, estado, expira_utc, observaciones)
            VALUES
                (@idempotency_key, @usuario_id, @request_hash, N'EnProceso', DATEADD(hour, 24, SYSUTCDATETIME()), N'ReversaVenta API');
            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """;

        await using var sqlCommand = new SqlCommand(sql, connection, transaction);
        sqlCommand.Parameters.Add("@idempotency_key", SqlDbType.UniqueIdentifier).Value = command.Request.IdempotencyKey!.Value;
        sqlCommand.Parameters.Add("@usuario_id", SqlDbType.Int).Value = command.UsuarioId;
        sqlCommand.Parameters.Add("@request_hash", SqlDbType.VarBinary, 32).Value = command.RequestHash;

        var result = await sqlCommand.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result);
    }

    private static async Task EnsureUsuarioActivoAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT_BIG(1)
            FROM dbo.usuario
            WHERE idUsuario = @usuario_id
              AND activo = 1;
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@usuario_id", SqlDbType.Int).Value = usuarioId;

        if (Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken)) != 1)
        {
            throw new VentaBusinessException(VentaServiceStatus.Invalid, "Usuario no autorizado para reversar venta.");
        }
    }

    private static async Task<EligibleVenta?> LoadEligibleVentaForUpdateAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int factura,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                v.factura,
                v.total,
                p.idPago,
                p.monto,
                mc.idMovimiento,
                mc.idTurno
            FROM dbo.ventas v WITH (UPDLOCK, HOLDLOCK)
            JOIN dbo.venta_pago p WITH (UPDLOCK, HOLDLOCK)
                ON p.factura = v.factura
               AND p.estado = N'Registrado'
               AND p.metodo_pago = N'Efectivo'
               AND p.moneda = @moneda
            JOIN dbo.movimiento_caja mc WITH (UPDLOCK, HOLDLOCK)
                ON mc.factura = v.factura
               AND mc.pago_id = p.idPago
               AND mc.tipo_movimiento = @tipo_venta_efectivo
               AND mc.estado = @estado_confirmado
            JOIN dbo.caja_turno ct WITH (UPDLOCK, HOLDLOCK)
                ON ct.idTurno = mc.idTurno
               AND ct.caja_codigo = @caja_codigo
               AND ct.estado = @estado_abierto
            WHERE v.factura = @factura
              AND v.metodo_pago = N'Efectivo'
              AND NOT EXISTS (
                  SELECT 1
                  FROM dbo.venta_pago px
                  WHERE px.factura = v.factura
                    AND px.estado = N'Registrado'
                    AND px.idPago <> p.idPago
              );
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@factura", SqlDbType.Int).Value = factura;
        command.Parameters.Add("@moneda", SqlDbType.Char, 3).Value = MonedaColones;
        command.Parameters.Add("@tipo_venta_efectivo", SqlDbType.NVarChar, 30).Value = TipoVentaEfectivo;
        command.Parameters.Add("@estado_confirmado", SqlDbType.NVarChar, 20).Value = EstadoConfirmado;
        command.Parameters.Add("@caja_codigo", SqlDbType.NVarChar, 50).Value = CajaCodigoVentaEfectivo;
        command.Parameters.Add("@estado_abierto", SqlDbType.NVarChar, 20).Value = EstadoTurnoAbierto;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var total = reader.GetDecimal(1);
        var pagoMonto = reader.GetDecimal(3);
        if (total <= 0 || pagoMonto != total)
        {
            return null;
        }

        return new EligibleVenta(
            reader.GetInt32(0),
            total,
            reader.GetInt64(2),
            reader.GetInt64(4),
            reader.GetInt64(5));
    }

    private static async Task<bool> VentaYaReversadaAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int factura,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT_BIG(1)
            FROM dbo.venta_reversa WITH (UPDLOCK, HOLDLOCK)
            WHERE factura = @factura
              AND estado = N'Confirmada';
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@factura", SqlDbType.Int).Value = factura;

        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    private static async Task<decimal> CalcularEfectivoDisponibleAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        long idTurno,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COALESCE(SUM(
                CASE
                    WHEN m.tipo_movimiento IN (N'FondoInicial', N'VentaEfectivo', N'IngresoCaja', N'AjustePositivo') THEN m.monto
                    WHEN m.tipo_movimiento IN (N'RetiroCaja', N'AjusteNegativo', N'DevolucionEfectivo') THEN -m.monto
                    WHEN m.tipo_movimiento = N'Reversa' AND original.tipo_movimiento IN (N'FondoInicial', N'VentaEfectivo', N'IngresoCaja', N'AjustePositivo') THEN -m.monto
                    WHEN m.tipo_movimiento = N'Reversa' AND original.tipo_movimiento IN (N'RetiroCaja', N'AjusteNegativo', N'DevolucionEfectivo') THEN m.monto
                    ELSE 0
                END), 0)
            FROM dbo.movimiento_caja m WITH (UPDLOCK, HOLDLOCK)
            LEFT JOIN dbo.movimiento_caja original
                ON original.idMovimiento = m.reversa_de_movimiento_id
            WHERE m.idTurno = @idTurno
              AND m.estado = @estado_confirmado
              AND m.tipo_movimiento <> N'CierreDiferencia';
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@idTurno", SqlDbType.BigInt).Value = idTurno;
        command.Parameters.Add("@estado_confirmado", SqlDbType.NVarChar, 20).Value = EstadoConfirmado;

        return Convert.ToDecimal(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task<IReadOnlyCollection<DetalleItem>> LoadDetalleForUpdateAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int factura,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT producto_id, cantidad
            FROM dbo.DetalleVenta WITH (UPDLOCK, HOLDLOCK)
            WHERE factura = @factura;
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@factura", SqlDbType.Int).Value = factura;

        var items = new List<DetalleItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new DetalleItem(reader.GetString(0), reader.GetInt32(1)));
        }

        return items;
    }

    private static async Task RestoreStockAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        DetalleItem item,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.inventario
            SET stock = ISNULL(stock, 0) + @cantidad,
                vendido = CASE WHEN ISNULL(vendido, 0) - @cantidad < 0 THEN 0 ELSE ISNULL(vendido, 0) - @cantidad END
            WHERE idProducto = @producto_id;
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@cantidad", SqlDbType.Int).Value = item.Cantidad;
        command.Parameters.Add("@producto_id", SqlDbType.NVarChar, 50).Value = item.ProductoId;

        if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
        {
            throw new VentaBusinessException(VentaServiceStatus.Conflict, "Inventario no elegible para reversa.");
        }
    }

    private static async Task<long> InsertMovimientoReversaAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        EligibleVenta venta,
        int usuarioId,
        string motivo,
        DateTime fechaHoraUtc,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.movimiento_caja (
                idTurno,
                tipo_movimiento,
                origen,
                monto,
                moneda,
                fecha_hora_utc,
                usuario_id,
                factura,
                pago_id,
                ingreso_caja_id,
                retiro_caja_id,
                referencia,
                observacion,
                estado,
                reversa_de_movimiento_id,
                correlacion_tecnica,
                creado_utc)
            OUTPUT INSERTED.idMovimiento
            VALUES (
                @idTurno,
                @tipo_movimiento,
                @origen,
                @monto,
                @moneda,
                @fecha_hora_utc,
                @usuario_id,
                @factura,
                @pago_id,
                NULL,
                NULL,
                @referencia,
                @observacion,
                @estado,
                @reversa_de_movimiento_id,
                NEWID(),
                SYSUTCDATETIME());
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@idTurno", SqlDbType.BigInt).Value = venta.IdTurno;
        command.Parameters.Add("@tipo_movimiento", SqlDbType.NVarChar, 30).Value = TipoReversa;
        command.Parameters.Add("@origen", SqlDbType.NVarChar, 30).Value = OrigenApi;
        command.Parameters.Add("@monto", SqlDbType.Decimal).Value = venta.Monto;
        command.Parameters["@monto"].Precision = 18;
        command.Parameters["@monto"].Scale = 2;
        command.Parameters.Add("@moneda", SqlDbType.Char, 3).Value = MonedaColones;
        command.Parameters.Add("@fecha_hora_utc", SqlDbType.DateTime2).Value = fechaHoraUtc;
        command.Parameters.Add("@usuario_id", SqlDbType.Int).Value = usuarioId;
        command.Parameters.Add("@factura", SqlDbType.Int).Value = venta.Factura;
        command.Parameters.Add("@pago_id", SqlDbType.BigInt).Value = venta.IdPago;
        command.Parameters.Add("@referencia", SqlDbType.NVarChar, 100).Value = "ReversaVentaEfectivo";
        command.Parameters.Add("@observacion", SqlDbType.NVarChar, 250).Value = motivo;
        command.Parameters.Add("@estado", SqlDbType.NVarChar, 20).Value = EstadoConfirmado;
        command.Parameters.Add("@reversa_de_movimiento_id", SqlDbType.BigInt).Value = venta.IdMovimientoVentaEfectivo;

        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task InsertVentaReversaAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        ReversarVentaPreparedCommand command,
        EligibleVenta venta,
        long idIdempotencia,
        long idMovimientoCompensatorio,
        DateTime fechaHoraUtc,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.venta_reversa
                (factura, idPago, idMovimientoVentaEfectivo, idMovimientoCompensatorio, idIdempotencia,
                 usuario_id, motivo, monto, moneda, estado, fecha_hora_utc, trace_id, observaciones)
            VALUES
                (@factura, @idPago, @idMovimientoVentaEfectivo, @idMovimientoCompensatorio, @idIdempotencia,
                 @usuario_id, @motivo, @monto, @moneda, N'Confirmada', @fecha_hora_utc, @trace_id, N'Reversa total VentaEfectivo API');
            """;

        await using var sqlCommand = new SqlCommand(sql, connection, transaction);
        sqlCommand.Parameters.Add("@factura", SqlDbType.Int).Value = venta.Factura;
        sqlCommand.Parameters.Add("@idPago", SqlDbType.BigInt).Value = venta.IdPago;
        sqlCommand.Parameters.Add("@idMovimientoVentaEfectivo", SqlDbType.BigInt).Value = venta.IdMovimientoVentaEfectivo;
        sqlCommand.Parameters.Add("@idMovimientoCompensatorio", SqlDbType.BigInt).Value = idMovimientoCompensatorio;
        sqlCommand.Parameters.Add("@idIdempotencia", SqlDbType.BigInt).Value = idIdempotencia;
        sqlCommand.Parameters.Add("@usuario_id", SqlDbType.Int).Value = command.UsuarioId;
        sqlCommand.Parameters.Add("@motivo", SqlDbType.NVarChar, 250).Value = command.Request.Motivo!.Trim();
        sqlCommand.Parameters.Add("@monto", SqlDbType.Decimal).Value = venta.Monto;
        sqlCommand.Parameters["@monto"].Precision = 18;
        sqlCommand.Parameters["@monto"].Scale = 2;
        sqlCommand.Parameters.Add("@moneda", SqlDbType.Char, 3).Value = MonedaColones;
        sqlCommand.Parameters.Add("@fecha_hora_utc", SqlDbType.DateTime2).Value = fechaHoraUtc;
        sqlCommand.Parameters.Add("@trace_id", SqlDbType.NVarChar, 100).Value =
            string.IsNullOrWhiteSpace(command.TraceId) ? DBNull.Value : command.TraceId;

        await sqlCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertAuditoriaAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        ReversarVentaPreparedCommand command,
        long idIdempotencia,
        DateTime fechaHoraUtc,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.venta_auditoria
                (factura, idIdempotencia, evento, usuario_id, fecha_hora_utc, origen, trace_id, detalle_json, observaciones)
            VALUES
                (@factura, @idIdempotencia, N'VentaReversada', @usuario_id, @fecha_hora_utc, N'POS.Api', @trace_id, N'{"evento":"VentaReversada"}', N'Reversa total VentaEfectivo API');
            """;

        await using var sqlCommand = new SqlCommand(sql, connection, transaction);
        sqlCommand.Parameters.Add("@factura", SqlDbType.Int).Value = command.Factura;
        sqlCommand.Parameters.Add("@idIdempotencia", SqlDbType.BigInt).Value = idIdempotencia;
        sqlCommand.Parameters.Add("@usuario_id", SqlDbType.Int).Value = command.UsuarioId;
        sqlCommand.Parameters.Add("@fecha_hora_utc", SqlDbType.DateTime2).Value = fechaHoraUtc;
        sqlCommand.Parameters.Add("@trace_id", SqlDbType.NVarChar, 100).Value =
            string.IsNullOrWhiteSpace(command.TraceId) ? DBNull.Value : command.TraceId;

        await sqlCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task CompleteIdempotenciaAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        long idIdempotencia,
        int factura,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.venta_idempotencia
            SET estado = N'Completada',
                factura = @factura,
                actualizado_utc = SYSUTCDATETIME(),
                error_code = NULL
            WHERE idIdempotencia = @idIdempotencia;
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@factura", SqlDbType.Int).Value = factura;
        command.Parameters.Add("@idIdempotencia", SqlDbType.BigInt).Value = idIdempotencia;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ResetFailedIdempotenciaAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        long idIdempotencia,
        byte[] requestHash,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.venta_idempotencia
            SET estado = N'EnProceso',
                request_hash = @request_hash,
                actualizado_utc = SYSUTCDATETIME(),
                error_code = NULL
            WHERE idIdempotencia = @idIdempotencia
              AND factura IS NULL;
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@request_hash", SqlDbType.VarBinary, 32).Value = requestHash;
        command.Parameters.Add("@idIdempotencia", SqlDbType.BigInt).Value = idIdempotencia;

        if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
        {
            throw new VentaBusinessException(VentaServiceStatus.Conflict, "Solicitud fallida no puede reintentarse.");
        }
    }

    private static async Task<ReversarVentaResponse?> LoadCompletedReversaResponseAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        long idIdempotencia,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT factura, monto, fecha_hora_utc, estado
            FROM dbo.venta_reversa
            WHERE idIdempotencia = @idIdempotencia;
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@idIdempotencia", SqlDbType.BigInt).Value = idIdempotencia;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ReversarVentaResponse(
            reader.GetString(3),
            reader.GetInt32(0),
            reader.GetDecimal(1),
            new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(2), DateTimeKind.Utc)),
            "Completada");
    }

    private static async Task<VentaIdempotenciaState?> ReadIdempotenciaAsync(
        SqlDataReader reader,
        CancellationToken cancellationToken)
    {
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new VentaIdempotenciaState(
            reader.GetInt64(0),
            reader.GetGuid(1),
            reader.GetInt32(2),
            (byte[])reader[3],
            reader.GetString(4),
            reader.IsDBNull(5) ? null : reader.GetInt32(5),
            reader.IsDBNull(6) ? null : (byte[])reader[6],
            reader.IsDBNull(7) ? null : reader.GetString(7),
            ToUtcOffset(reader.GetDateTime(8)),
            ToUtcOffset(reader.GetDateTime(9)),
            reader.IsDBNull(10) ? null : ToUtcOffset(reader.GetDateTime(10)));
    }

    private static DateTimeOffset ToUtcOffset(DateTime value)
    {
        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    private static bool IsUniqueConstraintViolation(SqlException exception)
    {
        foreach (SqlError error in exception.Errors)
        {
            if (error.Number is 2601 or 2627)
            {
                return true;
            }
        }

        return false;
    }

    private sealed record EligibleVenta(
        int Factura,
        decimal Monto,
        long IdPago,
        long IdMovimientoVentaEfectivo,
        long IdTurno);

    private sealed record DetalleItem(string ProductoId, int Cantidad);
}
