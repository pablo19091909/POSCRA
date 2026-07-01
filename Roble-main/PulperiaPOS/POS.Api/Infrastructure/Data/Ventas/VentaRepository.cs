using System.Data;
using Microsoft.Data.SqlClient;
using POS.Api.Application.Ventas;
using POS.Api.Contracts.Ventas;

namespace POS.Api.Infrastructure.Data.Ventas;

public sealed class VentaRepository : IVentaRepository
{
    private const string CajaCodigoVentaEfectivo = "CAJA_PRINCIPAL_TEST";
    private const string TipoVentaEfectivo = "VentaEfectivo";
    private const string EstadoTurnoAbierto = "Abierto";
    private const string EstadoMovimientoConfirmado = "Confirmado";
    private const string OrigenApi = "POS.Api";
    private const string MonedaColones = "CRC";

    private readonly IDatabaseConnectionFactory connectionFactory;

    public VentaRepository(IDatabaseConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<VentaIdempotenciaState?> GetIdempotenciaAsync(
        int usuarioId,
        Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(IdempotenciaSelectSql, connection);
        command.Parameters.Add("@usuario_id", SqlDbType.Int).Value = usuarioId;
        command.Parameters.Add("@idempotency_key", SqlDbType.UniqueIdentifier).Value = idempotencyKey;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await ReadIdempotenciaAsync(reader, cancellationToken);
    }

    public async Task<VentaServiceResult> CreateVentaTransactionalAsync(
        CrearVentaPreparedCommand command,
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
                var idempotenciaResult = await ResolveExistingIdempotenciaAsync(
                    connection,
                    (SqlTransaction)transaction,
                    command,
                    idempotencia,
                    cancellationToken);

                if (idempotenciaResult is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                    return idempotenciaResult;
                }
            }

            var idIdempotencia = idempotencia?.IdIdempotencia
                ?? await InsertIdempotenciaAsync(connection, (SqlTransaction)transaction, command, cancellationToken);

            long? idTurnoCaja = null;
            if (command.IntegrarCajaEfectivo)
            {
                idTurnoCaja = await GetTurnoAbiertoCajaForUpdateAsync(
                    connection,
                    (SqlTransaction)transaction,
                    CajaCodigoVentaEfectivo,
                    cancellationToken);
            }

            await EnsureUsuarioActivoAsync(connection, (SqlTransaction)transaction, command.UsuarioId, cancellationToken);
            await EnsureClienteExistsAsync(connection, (SqlTransaction)transaction, command.Request.ClienteId, cancellationToken);

            var items = await LoadItemsForUpdateAsync(connection, (SqlTransaction)transaction, command.Request.Items!, cancellationToken);
            var total = items.Sum(item => item.Subtotal);
            if (total <= 0)
            {
                throw new VentaBusinessException(VentaServiceStatus.Invalid, "Total de venta invalido.");
            }

            var payment = CalculatePayment(command.Request, total);
            var fechaHoraUtc = DateTime.UtcNow;

            var factura = await InsertVentaAsync(
                connection,
                (SqlTransaction)transaction,
                command,
                payment,
                total,
                fechaHoraUtc,
                cancellationToken);

            foreach (var item in items)
            {
                await InsertDetalleAsync(connection, (SqlTransaction)transaction, factura, item, cancellationToken);
                await DiscountStockAsync(connection, (SqlTransaction)transaction, item, cancellationToken);
            }

            if (string.Equals(payment.MetodoPago, "SaldoCliente", StringComparison.Ordinal))
            {
                await DiscountClienteSaldoAsync(
                    connection,
                    (SqlTransaction)transaction,
                    command.Request.ClienteId,
                    total,
                    cancellationToken);
            }

            var idPago = await InsertPagoAsync(
                connection,
                (SqlTransaction)transaction,
                factura,
                command.UsuarioId,
                payment,
                fechaHoraUtc,
                cancellationToken);

            if (command.IntegrarCajaEfectivo)
            {
                await InsertMovimientoVentaEfectivoAsync(
                    connection,
                    (SqlTransaction)transaction,
                    idTurnoCaja!.Value,
                    factura,
                    idPago,
                    command.UsuarioId,
                    payment.MontoVenta,
                    fechaHoraUtc,
                    cancellationToken);
            }

            await InsertAuditoriaAsync(
                connection,
                (SqlTransaction)transaction,
                factura,
                idIdempotencia,
                command.UsuarioId,
                command.TraceId,
                fechaHoraUtc,
                cancellationToken);

            await CompleteIdempotenciaAsync(
                connection,
                (SqlTransaction)transaction,
                idIdempotencia,
                factura,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return VentaServiceResult.Success(BuildResponse(factura, fechaHoraUtc, items, payment, "Nueva"));
        }
        catch (VentaBusinessException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return ex.Status == VentaServiceStatus.Conflict
                ? VentaServiceResult.Conflict(ex.SafeMessage)
                : VentaServiceResult.Invalid([ex.SafeMessage]);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            return VentaServiceResult.Invalid(["No fue posible completar la venta."]);
        }
    }

    private static async Task<VentaIdempotenciaState?> GetIdempotenciaForUpdateAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        CrearVentaPreparedCommand command,
        CancellationToken cancellationToken)
    {
        await using var sqlCommand = new SqlCommand(IdempotenciaForUpdateSql, connection, transaction);
        sqlCommand.Parameters.Add("@usuario_id", SqlDbType.Int).Value = command.UsuarioId;
        sqlCommand.Parameters.Add("@idempotency_key", SqlDbType.UniqueIdentifier).Value = command.Request.IdempotencyKey!.Value;

        await using var reader = await sqlCommand.ExecuteReaderAsync(cancellationToken);
        return await ReadIdempotenciaAsync(reader, cancellationToken);
    }

    private static async Task<VentaServiceResult?> ResolveExistingIdempotenciaAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        CrearVentaPreparedCommand command,
        VentaIdempotenciaState state,
        CancellationToken cancellationToken)
    {
        if (!state.RequestHash.SequenceEqual(command.RequestHash))
        {
            return VentaServiceResult.Conflict("Idempotency key ya fue usada con una solicitud distinta.");
        }

        if (string.Equals(state.Estado, "EnProceso", StringComparison.Ordinal))
        {
            return VentaServiceResult.InProgress();
        }

        if (string.Equals(state.Estado, "Completada", StringComparison.Ordinal) && state.Factura is not null)
        {
            var response = await LoadCompletedVentaResponseAsync(connection, transaction, state.Factura.Value, cancellationToken);
            return response is null
                ? VentaServiceResult.Conflict("No fue posible recuperar la venta completada.")
                : VentaServiceResult.Success(response with { ResultadoIdempotencia = "Repetida" });
        }

        if (string.Equals(state.Estado, "Fallida", StringComparison.Ordinal) && state.Factura is null)
        {
            await ResetFailedIdempotenciaAsync(connection, transaction, state.IdIdempotencia, command.RequestHash, cancellationToken);
            return null;
        }

        return VentaServiceResult.Conflict("Estado de idempotencia no permite continuar.");
    }

    private static async Task<long> InsertIdempotenciaAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        CrearVentaPreparedCommand command,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.venta_idempotencia
                (idempotency_key, usuario_id, request_hash, estado, expira_utc, observaciones)
            VALUES
                (@idempotency_key, @usuario_id, @request_hash, N'EnProceso', DATEADD(hour, 24, SYSUTCDATETIME()), N'Venta API');
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

        var exists = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken)) == 1;
        if (!exists)
        {
            throw new VentaBusinessException(VentaServiceStatus.Invalid, "Usuario no autorizado para registrar venta.");
        }
    }

    private static async Task EnsureClienteExistsAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int clienteId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT_BIG(1)
            FROM dbo.cliente WITH (UPDLOCK, HOLDLOCK)
            WHERE idCliente = @cliente_id;
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@cliente_id", SqlDbType.Int).Value = clienteId;

        var exists = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken)) == 1;
        if (!exists)
        {
            throw new VentaBusinessException(VentaServiceStatus.Invalid, "Cliente no encontrado.");
        }
    }

    private static async Task<long> GetTurnoAbiertoCajaForUpdateAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string cajaCodigo,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (1) idTurno
            FROM dbo.caja_turno WITH (UPDLOCK, HOLDLOCK)
            WHERE caja_codigo = @caja_codigo
              AND estado = @estado_abierto
            ORDER BY apertura_utc DESC, idTurno DESC;
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@caja_codigo", SqlDbType.NVarChar, 50).Value = cajaCodigo;
        command.Parameters.Add("@estado_abierto", SqlDbType.NVarChar, 20).Value = EstadoTurnoAbierto;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null || result == DBNull.Value)
        {
            throw new VentaBusinessException(VentaServiceStatus.Invalid, "No hay un turno abierto para registrar venta en efectivo.");
        }

        return Convert.ToInt64(result);
    }

    private static async Task<IReadOnlyCollection<VentaTransactionItem>> LoadItemsForUpdateAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        IReadOnlyCollection<VentaItemRequest> requestItems,
        CancellationToken cancellationToken)
    {
        var items = new List<VentaTransactionItem>();
        foreach (var requestItem in requestItems)
        {
            const string sql = """
                SELECT nombre, precio
                FROM dbo.inventario WITH (UPDLOCK, HOLDLOCK)
                WHERE idProducto = @producto_id;
                """;

            await using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.Add("@producto_id", SqlDbType.NVarChar, 50).Value = requestItem.ProductoId!.Trim();

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new VentaBusinessException(VentaServiceStatus.Invalid, "Producto no encontrado.");
            }

            var precio = reader.IsDBNull(1) ? 0m : reader.GetDecimal(1);
            if (precio <= 0)
            {
                throw new VentaBusinessException(VentaServiceStatus.Invalid, "Producto no disponible para venta.");
            }

            items.Add(new VentaTransactionItem(
                requestItem.ProductoId!.Trim(),
                reader.GetString(0),
                requestItem.Cantidad,
                precio));
        }

        return items;
    }

    private static VentaPaymentCalculation CalculatePayment(CrearVentaRequest request, decimal total)
    {
        var pago = request.Pago!;
        var metodo = pago.MetodoPago!.Trim();
        var referencia = FirstNonEmpty(pago.Referencia, request.ReferenciaPago);
        var voucher = FirstNonEmpty(pago.Voucher, request.Voucher);

        return metodo switch
        {
            "Efectivo" => CalculateEfectivo(pago, total),
            "Tarjeta" => CalculateTarjeta(pago, total, voucher),
            "Sinpe" => CalculateSinpe(pago, total, referencia),
            "SaldoCliente" => CalculateSaldoCliente(pago, total),
            "Dolares" => throw new VentaBusinessException(VentaServiceStatus.Invalid, "Pago en dolares no habilitado hasta contar con tipo de cambio decimal server-side."),
            _ => throw new VentaBusinessException(VentaServiceStatus.Invalid, "Metodo de pago no soportado.")
        };
    }

    private static VentaPaymentCalculation CalculateEfectivo(PagoVentaRequest pago, decimal total)
    {
        if (pago.MontoRecibido is null || pago.MontoRecibido < total)
        {
            throw new VentaBusinessException(VentaServiceStatus.Invalid, "Monto recibido insuficiente.");
        }

        var vuelto = pago.MontoRecibido.Value - total;
        return new VentaPaymentCalculation("Efectivo", "CRC", total, pago.MontoRecibido, vuelto, null, null, null, null, null, pago.MontoRecibido.Value);
    }

    private static VentaPaymentCalculation CalculateTarjeta(PagoVentaRequest pago, decimal total, string? voucher)
    {
        if (string.IsNullOrWhiteSpace(voucher))
        {
            throw new VentaBusinessException(VentaServiceStatus.Invalid, "Voucher requerido para pago con tarjeta.");
        }

        if (pago.MontoRecibido is not null && pago.MontoRecibido != total)
        {
            throw new VentaBusinessException(VentaServiceStatus.Invalid, "Monto recibido invalido para tarjeta.");
        }

        return new VentaPaymentCalculation("Tarjeta", "CRC", total, total, null, null, null, voucher.Trim(), voucher.Trim(), null, total);
    }

    private static VentaPaymentCalculation CalculateSinpe(PagoVentaRequest pago, decimal total, string? referencia)
    {
        if (string.IsNullOrWhiteSpace(referencia))
        {
            throw new VentaBusinessException(VentaServiceStatus.Invalid, "Referencia requerida para pago Sinpe.");
        }

        if (pago.MontoRecibido is not null && pago.MontoRecibido != total)
        {
            throw new VentaBusinessException(VentaServiceStatus.Invalid, "Monto recibido invalido para Sinpe.");
        }

        return new VentaPaymentCalculation("Sinpe", "CRC", total, total, null, null, referencia.Trim(), null, null, referencia.Trim(), total);
    }

    private static VentaPaymentCalculation CalculateSaldoCliente(PagoVentaRequest pago, decimal total)
    {
        if (pago.MontoRecibido is not null)
        {
            throw new VentaBusinessException(VentaServiceStatus.Invalid, "SaldoCliente no admite monto recibido.");
        }

        return new VentaPaymentCalculation("SaldoCliente", "CRC", total, null, null, null, null, null, null, null, total);
    }

    private static async Task<int> InsertVentaAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        CrearVentaPreparedCommand command,
        VentaPaymentCalculation payment,
        decimal total,
        DateTime fechaHoraUtc,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.ventas
                (cliente_id, total, fecha, hora, usuario_id, metodo_pago, numero_voucher, numero_comprobante, monto_pagado, vuelto)
            VALUES
                (@cliente_id, @total, @fecha, @hora, @usuario_id, @metodo_pago, @numero_voucher, @numero_comprobante, @monto_pagado, @vuelto);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        await using var sqlCommand = new SqlCommand(sql, connection, transaction);
        sqlCommand.Parameters.Add("@cliente_id", SqlDbType.Int).Value = command.Request.ClienteId;
        sqlCommand.Parameters.Add("@total", SqlDbType.Decimal).Value = total;
        sqlCommand.Parameters["@total"].Precision = 10;
        sqlCommand.Parameters["@total"].Scale = 2;
        sqlCommand.Parameters.Add("@fecha", SqlDbType.Date).Value = fechaHoraUtc.Date;
        sqlCommand.Parameters.Add("@hora", SqlDbType.Time).Value = fechaHoraUtc.TimeOfDay;
        sqlCommand.Parameters.Add("@usuario_id", SqlDbType.Int).Value = command.UsuarioId;
        sqlCommand.Parameters.Add("@metodo_pago", SqlDbType.NVarChar, 50).Value = payment.MetodoPago;
        sqlCommand.Parameters.Add("@numero_voucher", SqlDbType.NVarChar, 100).Value = (object?)payment.NumeroVoucherVenta ?? DBNull.Value;
        sqlCommand.Parameters.Add("@numero_comprobante", SqlDbType.NVarChar, 100).Value = (object?)payment.NumeroComprobanteVenta ?? DBNull.Value;
        sqlCommand.Parameters.Add("@monto_pagado", SqlDbType.Decimal).Value = payment.MontoPagadoVenta;
        sqlCommand.Parameters["@monto_pagado"].Precision = 10;
        sqlCommand.Parameters["@monto_pagado"].Scale = 2;
        sqlCommand.Parameters.Add("@vuelto", SqlDbType.Decimal).Value = (object?)payment.Vuelto ?? 0m;
        sqlCommand.Parameters["@vuelto"].Precision = 10;
        sqlCommand.Parameters["@vuelto"].Scale = 2;

        var result = await sqlCommand.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    private static async Task InsertDetalleAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int factura,
        VentaTransactionItem item,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.DetalleVenta (factura, producto_id, cantidad, precio_unitario)
            VALUES (@factura, @producto_id, @cantidad, @precio_unitario);
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@factura", SqlDbType.Int).Value = factura;
        command.Parameters.Add("@producto_id", SqlDbType.NVarChar, 50).Value = item.ProductoId;
        command.Parameters.Add("@cantidad", SqlDbType.Int).Value = item.Cantidad;
        command.Parameters.Add("@precio_unitario", SqlDbType.Decimal).Value = item.PrecioUnitario;
        command.Parameters["@precio_unitario"].Precision = 10;
        command.Parameters["@precio_unitario"].Scale = 2;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task DiscountStockAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        VentaTransactionItem item,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.inventario
            SET stock = stock - @cantidad,
                vendido = ISNULL(vendido, 0) + @cantidad
            WHERE idProducto = @producto_id
              AND ISNULL(stock, 0) >= @cantidad;
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@cantidad", SqlDbType.Int).Value = item.Cantidad;
        command.Parameters.Add("@producto_id", SqlDbType.NVarChar, 50).Value = item.ProductoId;

        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        if (affected != 1)
        {
            throw new VentaBusinessException(VentaServiceStatus.Invalid, "Stock insuficiente.");
        }
    }

    private static async Task DiscountClienteSaldoAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int clienteId,
        decimal total,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.cliente
            SET saldo = saldo - @total
            WHERE idCliente = @cliente_id
              AND saldo >= @total;
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@cliente_id", SqlDbType.Int).Value = clienteId;
        command.Parameters.Add("@total", SqlDbType.Decimal).Value = total;
        command.Parameters["@total"].Precision = 10;
        command.Parameters["@total"].Scale = 2;

        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        if (affected != 1)
        {
            throw new VentaBusinessException(VentaServiceStatus.Invalid, "Saldo insuficiente.");
        }
    }

    private static async Task<long> InsertPagoAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int factura,
        int usuarioId,
        VentaPaymentCalculation payment,
        DateTime fechaHoraUtc,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.venta_pago
                (factura, metodo_pago, moneda, monto, monto_recibido, vuelto, tipo_cambio_aplicado, referencia, voucher, fecha_hora_utc, usuario_id, estado)
            OUTPUT INSERTED.idPago
            VALUES
                (@factura, @metodo_pago, @moneda, @monto, @monto_recibido, @vuelto, @tipo_cambio_aplicado, @referencia, @voucher, @fecha_hora_utc, @usuario_id, N'Registrado');
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@factura", SqlDbType.Int).Value = factura;
        command.Parameters.Add("@metodo_pago", SqlDbType.NVarChar, 30).Value = payment.MetodoPago;
        command.Parameters.Add("@moneda", SqlDbType.Char, 3).Value = payment.Moneda;
        command.Parameters.Add("@monto", SqlDbType.Decimal).Value = payment.MontoVenta;
        command.Parameters["@monto"].Precision = 18;
        command.Parameters["@monto"].Scale = 2;
        command.Parameters.Add("@monto_recibido", SqlDbType.Decimal).Value = (object?)payment.MontoRecibido ?? DBNull.Value;
        command.Parameters["@monto_recibido"].Precision = 18;
        command.Parameters["@monto_recibido"].Scale = 2;
        command.Parameters.Add("@vuelto", SqlDbType.Decimal).Value = (object?)payment.Vuelto ?? DBNull.Value;
        command.Parameters["@vuelto"].Precision = 18;
        command.Parameters["@vuelto"].Scale = 2;
        command.Parameters.Add("@tipo_cambio_aplicado", SqlDbType.Decimal).Value = (object?)payment.TipoCambioAplicado ?? DBNull.Value;
        command.Parameters["@tipo_cambio_aplicado"].Precision = 18;
        command.Parameters["@tipo_cambio_aplicado"].Scale = 6;
        command.Parameters.Add("@referencia", SqlDbType.NVarChar, 100).Value = (object?)payment.Referencia ?? DBNull.Value;
        command.Parameters.Add("@voucher", SqlDbType.NVarChar, 100).Value = (object?)payment.Voucher ?? DBNull.Value;
        command.Parameters.Add("@fecha_hora_utc", SqlDbType.DateTime2).Value = fechaHoraUtc;
        command.Parameters.Add("@usuario_id", SqlDbType.Int).Value = usuarioId;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result);
    }

    private static async Task InsertMovimientoVentaEfectivoAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        long idTurno,
        int factura,
        long idPago,
        int usuarioId,
        decimal monto,
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
                NULL,
                NEWID(),
                SYSUTCDATETIME());
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@idTurno", SqlDbType.BigInt).Value = idTurno;
        command.Parameters.Add("@tipo_movimiento", SqlDbType.NVarChar, 30).Value = TipoVentaEfectivo;
        command.Parameters.Add("@origen", SqlDbType.NVarChar, 30).Value = OrigenApi;
        command.Parameters.Add("@monto", SqlDbType.Decimal).Value = monto;
        command.Parameters["@monto"].Precision = 18;
        command.Parameters["@monto"].Scale = 2;
        command.Parameters.Add("@moneda", SqlDbType.Char, 3).Value = MonedaColones;
        command.Parameters.Add("@fecha_hora_utc", SqlDbType.DateTime2).Value = fechaHoraUtc;
        command.Parameters.Add("@usuario_id", SqlDbType.Int).Value = usuarioId;
        command.Parameters.Add("@factura", SqlDbType.Int).Value = factura;
        command.Parameters.Add("@pago_id", SqlDbType.BigInt).Value = idPago;
        command.Parameters.Add("@referencia", SqlDbType.NVarChar, 100).Value = "VentaEfectivo";
        command.Parameters.Add("@observacion", SqlDbType.NVarChar, 250).Value = "Venta API efectivo";
        command.Parameters.Add("@estado", SqlDbType.NVarChar, 20).Value = EstadoMovimientoConfirmado;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertAuditoriaAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int factura,
        long idIdempotencia,
        int usuarioId,
        string traceId,
        DateTime fechaHoraUtc,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.venta_auditoria
                (factura, idIdempotencia, evento, usuario_id, fecha_hora_utc, origen, trace_id, detalle_json, observaciones)
            VALUES
                (@factura, @idIdempotencia, N'VentaCreada', @usuario_id, @fecha_hora_utc, N'POS.Api', @trace_id, N'{"evento":"VentaCreada"}', N'Venta API transaccional');
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@factura", SqlDbType.Int).Value = factura;
        command.Parameters.Add("@idIdempotencia", SqlDbType.BigInt).Value = idIdempotencia;
        command.Parameters.Add("@usuario_id", SqlDbType.Int).Value = usuarioId;
        command.Parameters.Add("@fecha_hora_utc", SqlDbType.DateTime2).Value = fechaHoraUtc;
        command.Parameters.Add("@trace_id", SqlDbType.NVarChar, 100).Value = string.IsNullOrWhiteSpace(traceId) ? DBNull.Value : traceId;

        await command.ExecuteNonQueryAsync(cancellationToken);
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

        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        if (affected != 1)
        {
            throw new VentaBusinessException(VentaServiceStatus.Conflict, "Solicitud fallida no puede reintentarse.");
        }
    }

    private static async Task<VentaResponse?> LoadCompletedVentaResponseAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int factura,
        CancellationToken cancellationToken)
    {
        const string ventaSql = """
            SELECT total, metodo_pago, monto_pagado, vuelto, fecha, hora
            FROM dbo.ventas
            WHERE factura = @factura;
            """;

        decimal total;
        string metodoPago;
        decimal montoPagado;
        decimal? vuelto;
        DateTime fecha;
        TimeSpan hora;

        await using (var command = new SqlCommand(ventaSql, connection, transaction))
        {
            command.Parameters.Add("@factura", SqlDbType.Int).Value = factura;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            total = reader.IsDBNull(0) ? 0m : reader.GetDecimal(0);
            metodoPago = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            montoPagado = reader.IsDBNull(2) ? total : reader.GetDecimal(2);
            vuelto = reader.IsDBNull(3) ? null : reader.GetDecimal(3);
            fecha = reader.GetDateTime(4);
            hora = reader.GetTimeSpan(5);
        }

        var items = await LoadCompletedItemsAsync(connection, transaction, factura, cancellationToken);
        var fechaHoraUtc = new DateTimeOffset(DateTime.SpecifyKind(fecha.Date.Add(hora), DateTimeKind.Utc));
        var payment = new PagoVentaResponse(metodoPago, "CRC", total, montoPagado, vuelto, null);

        return new VentaResponse(factura, "Completada", total, montoPagado, vuelto, metodoPago, fechaHoraUtc, "Completada", items, payment);
    }

    private static async Task<IReadOnlyCollection<VentaItemResponse>> LoadCompletedItemsAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int factura,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT d.producto_id, i.nombre, d.cantidad, d.precio_unitario
            FROM dbo.DetalleVenta d
            JOIN dbo.inventario i ON i.idProducto = d.producto_id
            WHERE d.factura = @factura
            ORDER BY d.idDetalle;
            """;

        var items = new List<VentaItemResponse>();
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@factura", SqlDbType.Int).Value = factura;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var cantidad = reader.GetInt32(2);
            var precio = reader.GetDecimal(3);
            items.Add(new VentaItemResponse(reader.GetString(0), reader.GetString(1), cantidad, precio, cantidad * precio));
        }

        return items;
    }

    private static VentaResponse BuildResponse(
        int factura,
        DateTime fechaHoraUtc,
        IReadOnlyCollection<VentaTransactionItem> items,
        VentaPaymentCalculation payment,
        string resultadoIdempotencia)
    {
        var responseItems = items
            .Select(item => new VentaItemResponse(item.ProductoId, item.Nombre, item.Cantidad, item.PrecioUnitario, item.Subtotal))
            .ToArray();

        var pago = new PagoVentaResponse(
            payment.MetodoPago,
            payment.Moneda,
            payment.MontoVenta,
            payment.MontoRecibido,
            payment.Vuelto,
            payment.TipoCambioAplicado);

        return new VentaResponse(
            factura,
            "Completada",
            payment.MontoVenta,
            payment.MontoPagadoVenta,
            payment.Vuelto,
            payment.MetodoPago,
            new DateTimeOffset(DateTime.SpecifyKind(fechaHoraUtc, DateTimeKind.Utc)),
            resultadoIdempotencia,
            responseItems,
            pago);
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

    private static string? FirstNonEmpty(string? first, string? second)
    {
        return !string.IsNullOrWhiteSpace(first) ? first : second;
    }

    private static DateTimeOffset ToUtcOffset(DateTime value)
    {
        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    private const string IdempotenciaSelectSql = """
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
        FROM dbo.venta_idempotencia
        WHERE usuario_id = @usuario_id
          AND idempotency_key = @idempotency_key;
        """;

    private const string IdempotenciaForUpdateSql = """
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
}
