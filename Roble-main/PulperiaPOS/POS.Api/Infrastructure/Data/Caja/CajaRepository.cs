using System.Data;
using Microsoft.Data.SqlClient;
using POS.Api.Application.Caja;

namespace POS.Api.Infrastructure.Data.Caja;

public sealed class CajaRepository : ICajaRepository
{
    private const string EstadoAbierto = "Abierto";
    private const string EstadoEnCierre = "EnCierre";
    private const string EstadoCerrado = "Cerrado";
    private const string EstadoConfirmado = "Confirmado";
    private const string MonedaColones = "CRC";
    private const string OrigenApi = "POS.Api";
    private const string TipoFondoInicial = "FondoInicial";
    private const string TipoIngresoCaja = "IngresoCaja";
    private const string TipoRetiroCaja = "RetiroCaja";
    private const string TipoCierreDiferencia = "CierreDiferencia";
    private const string ResultadoIngresoCajaCreado = "IngresoCajaCreado";
    private const string ResultadoRetiroCajaCreado = "RetiroCajaCreado";
    private const string ResultadoTurnoAbierto = "TurnoAbierto";
    private const string ResultadoTurnoCerrado = "TurnoCerrado";

    private readonly IDatabaseConnectionFactory connectionFactory;

    public CajaRepository(IDatabaseConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<CajaTurnoQuery?> GetTurnoAbiertoAsync(string cajaCodigo, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT TOP (1)
                idTurno, caja_codigo, estado, usuario_apertura_id, usuario_cierre_id,
                apertura_utc, cierre_utc, fondo_inicial, efectivo_esperado,
                efectivo_contado, diferencia, observacion_apertura,
                observacion_cierre, cierre_caja_id, row_version
            FROM dbo.caja_turno
            WHERE caja_codigo = @caja_codigo
              AND estado IN (N'Abierto', N'EnCierre')
            ORDER BY apertura_utc DESC, idTurno DESC;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@caja_codigo", cajaCodigo);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapTurno(reader) : null;
    }

    public async Task<CajaTurnoQuery> AbrirTurnoAsync(
        string cajaCodigo,
        decimal fondoInicial,
        string? observacion,
        int usuarioId,
        Guid idempotencyKey,
        byte[] requestHash,
        CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            if (!await UsuarioActivoExisteAsync(connection, transaction, usuarioId, cancellationToken))
            {
                throw new CajaBusinessException(CajaServiceStatus.Invalid, "Usuario invalido.");
            }

            var idempotencia = await GetCajaIdempotenciaForUpdateAsync(
                connection,
                transaction,
                usuarioId,
                CajaIdempotencyOperation.AbrirTurno.ToString(),
                idempotencyKey,
                cancellationToken);

            if (idempotencia is not null)
            {
                var existing = await ResolveExistingTurnoIdempotenciaAsync(
                    connection,
                    transaction,
                    idempotencia,
                    requestHash,
                    cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                return existing;
            }

            if (await TurnoAbiertoExisteAsync(connection, transaction, cajaCodigo, cancellationToken))
            {
                throw new CajaBusinessException(CajaServiceStatus.Conflict, "Ya existe un turno abierto para esta caja.");
            }

            var idCajaIdempotencia = await CrearCajaIdempotenciaEnProcesoAsync(
                connection,
                transaction,
                usuarioId,
                null,
                cajaCodigo,
                CajaIdempotencyOperation.AbrirTurno.ToString(),
                idempotencyKey,
                requestHash,
                cancellationToken);

            var idTurno = await CrearTurnoAsync(
                connection,
                transaction,
                cajaCodigo,
                fondoInicial,
                observacion,
                usuarioId,
                cancellationToken);

            var idMovimiento = await CrearMovimientoFondoInicialAsync(
                connection,
                transaction,
                idTurno,
                cajaCodigo,
                fondoInicial,
                observacion,
                usuarioId,
                cancellationToken);

            await CompletarCajaIdempotenciaAsync(
                connection,
                transaction,
                idCajaIdempotencia,
                idTurno,
                idMovimiento,
                ResultadoTurnoAbierto,
                cancellationToken);

            var turno = await GetTurnoByIdAsync(connection, transaction, idTurno, cancellationToken)
                ?? throw new InvalidOperationException("Caja turn was not found after creation.");

            await transaction.CommitAsync(cancellationToken);
            return turno;
        }
        catch (SqlException exception) when (IsUniqueConstraintViolation(exception))
        {
            await RollbackQuietlyAsync(transaction, cancellationToken);
            throw new CajaBusinessException(CajaServiceStatus.Conflict, "Ya existe un turno abierto para esta caja.");
        }
        catch
        {
            await RollbackQuietlyAsync(transaction, cancellationToken);
            throw;
        }
    }

    public async Task<MovimientoCajaQuery> RegistrarIngresoAsync(
        string cajaCodigo,
        decimal monto,
        string motivo,
        string? referencia,
        int usuarioId,
        Guid idempotencyKey,
        byte[] requestHash,
        CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            if (!await UsuarioActivoExisteAsync(connection, transaction, usuarioId, cancellationToken))
            {
                throw new CajaBusinessException(CajaServiceStatus.Invalid, "Usuario invalido.");
            }

            var idempotencia = await GetCajaIdempotenciaForUpdateAsync(
                connection,
                transaction,
                usuarioId,
                TipoIngresoCaja,
                idempotencyKey,
                cancellationToken);

            if (idempotencia is not null)
            {
                var existing = await ResolveExistingCajaIdempotenciaAsync(
                    connection,
                    transaction,
                    idempotencia,
                    requestHash,
                    cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                return existing;
            }

            var idTurno = await GetTurnoAbiertoIdForUpdateAsync(connection, transaction, cajaCodigo, cancellationToken);
            if (idTurno is null)
            {
                throw new CajaBusinessException(CajaServiceStatus.Conflict, "No existe turno abierto para esta caja.");
            }

            var idCajaIdempotencia = await CrearCajaIdempotenciaEnProcesoAsync(
                connection,
                transaction,
                usuarioId,
                idTurno.Value,
                cajaCodigo,
                TipoIngresoCaja,
                idempotencyKey,
                requestHash,
                cancellationToken);

            var idMovimiento = await CrearMovimientoIngresoCajaAsync(
                connection,
                transaction,
                idTurno.Value,
                monto,
                motivo,
                referencia,
                usuarioId,
                cancellationToken);

            await CompletarCajaIdempotenciaAsync(
                connection,
                transaction,
                idCajaIdempotencia,
                idTurno.Value,
                idMovimiento,
                ResultadoIngresoCajaCreado,
                cancellationToken);

            var movimiento = await GetMovimientoByIdAsync(connection, transaction, idMovimiento, cancellationToken)
                ?? throw new InvalidOperationException("Cash income movement was not found after creation.");

            await transaction.CommitAsync(cancellationToken);
            return movimiento;
        }
        catch (SqlException exception) when (IsUniqueConstraintViolation(exception))
        {
            await RollbackQuietlyAsync(transaction, cancellationToken);
            throw new CajaBusinessException(CajaServiceStatus.Conflict, "Solicitud de caja duplicada o en conflicto.");
        }
        catch
        {
            await RollbackQuietlyAsync(transaction, cancellationToken);
            throw;
        }
    }

    public async Task<MovimientoCajaQuery> RegistrarRetiroAsync(
        string cajaCodigo,
        decimal monto,
        string motivo,
        string? referencia,
        int usuarioId,
        Guid idempotencyKey,
        byte[] requestHash,
        CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            if (!await UsuarioActivoExisteAsync(connection, transaction, usuarioId, cancellationToken))
            {
                throw new CajaBusinessException(CajaServiceStatus.Invalid, "Usuario invalido.");
            }

            var idempotencia = await GetCajaIdempotenciaForUpdateAsync(
                connection,
                transaction,
                usuarioId,
                TipoRetiroCaja,
                idempotencyKey,
                cancellationToken);

            if (idempotencia is not null)
            {
                var existing = await ResolveExistingCajaIdempotenciaAsync(
                    connection,
                    transaction,
                    idempotencia,
                    requestHash,
                    cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                return existing;
            }

            var idTurno = await GetTurnoAbiertoIdForUpdateAsync(connection, transaction, cajaCodigo, cancellationToken);
            if (idTurno is null)
            {
                throw new CajaBusinessException(CajaServiceStatus.Conflict, "No existe turno abierto para esta caja.");
            }

            var efectivoDisponible = await CalcularEfectivoDisponibleEnTurnoAsync(
                connection,
                transaction,
                idTurno.Value,
                cancellationToken);

            if (monto > efectivoDisponible)
            {
                throw new CajaBusinessException(CajaServiceStatus.Conflict, "Efectivo insuficiente para registrar el retiro.");
            }

            var idCajaIdempotencia = await CrearCajaIdempotenciaEnProcesoAsync(
                connection,
                transaction,
                usuarioId,
                idTurno.Value,
                cajaCodigo,
                TipoRetiroCaja,
                idempotencyKey,
                requestHash,
                cancellationToken);

            var idMovimiento = await CrearMovimientoRetiroCajaAsync(
                connection,
                transaction,
                idTurno.Value,
                monto,
                motivo,
                referencia,
                usuarioId,
                cancellationToken);

            await CompletarCajaIdempotenciaAsync(
                connection,
                transaction,
                idCajaIdempotencia,
                idTurno.Value,
                idMovimiento,
                ResultadoRetiroCajaCreado,
                cancellationToken);

            var movimiento = await GetMovimientoByIdAsync(connection, transaction, idMovimiento, cancellationToken)
                ?? throw new InvalidOperationException("Cash withdrawal movement was not found after creation.");

            await transaction.CommitAsync(cancellationToken);
            return movimiento;
        }
        catch (SqlException exception) when (IsUniqueConstraintViolation(exception))
        {
            await RollbackQuietlyAsync(transaction, cancellationToken);
            throw new CajaBusinessException(CajaServiceStatus.Conflict, "Solicitud de caja duplicada o en conflicto.");
        }
        catch
        {
            await RollbackQuietlyAsync(transaction, cancellationToken);
            throw;
        }
    }

    public async Task<CajaTurnoQuery?> GetTurnoByIdAsync(long idTurno, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                idTurno, caja_codigo, estado, usuario_apertura_id, usuario_cierre_id,
                apertura_utc, cierre_utc, fondo_inicial, efectivo_esperado,
                efectivo_contado, diferencia, observacion_apertura,
                observacion_cierre, cierre_caja_id, row_version
            FROM dbo.caja_turno
            WHERE idTurno = @idTurno;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@idTurno", idTurno);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapTurno(reader) : null;
    }

    public async Task<IReadOnlyCollection<MovimientoCajaQuery>> GetMovimientosAsync(
        long idTurno,
        int limit,
        CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT TOP (@limit)
                idMovimiento, idTurno, tipo_movimiento, origen, monto, moneda,
                fecha_hora_utc, usuario_id, factura, pago_id, ingreso_caja_id,
                retiro_caja_id, referencia, observacion, estado,
                reversa_de_movimiento_id
            FROM dbo.movimiento_caja
            WHERE idTurno = @idTurno
            ORDER BY fecha_hora_utc, idMovimiento;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@idTurno", idTurno);
        command.Parameters.AddWithValue("@limit", limit);

        var movimientos = new List<MovimientoCajaQuery>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            movimientos.Add(MapMovimiento(reader));
        }

        return movimientos;
    }

    public async Task<IReadOnlyCollection<ResumenMovimientoCajaQuery>> GetResumenMovimientosAsync(
        long idTurno,
        CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                tipo_movimiento,
                COUNT_BIG(1) AS cantidad,
                SUM(monto) AS total
            FROM dbo.movimiento_caja
            WHERE idTurno = @idTurno
              AND estado = N'Confirmado'
              AND tipo_movimiento <> N'CierreDiferencia'
            GROUP BY tipo_movimiento
            ORDER BY tipo_movimiento;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@idTurno", idTurno);

        var resumen = new List<ResumenMovimientoCajaQuery>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            resumen.Add(new ResumenMovimientoCajaQuery(
                reader.GetString(0),
                Convert.ToInt32(reader.GetInt64(1)),
                reader.IsDBNull(2) ? 0 : reader.GetDecimal(2)));
        }

        return resumen;
    }

    private static async Task<IReadOnlyCollection<ResumenMovimientoCajaQuery>> GetResumenMovimientosAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        long idTurno,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                tipo_movimiento,
                COUNT_BIG(1) AS cantidad,
                SUM(monto) AS total
            FROM dbo.movimiento_caja
            WHERE idTurno = @idTurno
              AND estado = @estado
              AND tipo_movimiento <> @tipo_cierre_diferencia
            GROUP BY tipo_movimiento
            ORDER BY tipo_movimiento;
            """;

        await using var command = CreateCommand(connection, transaction, sql);
        command.Parameters.Add("@idTurno", SqlDbType.BigInt).Value = idTurno;
        command.Parameters.Add("@estado", SqlDbType.NVarChar, 20).Value = EstadoConfirmado;
        command.Parameters.Add("@tipo_cierre_diferencia", SqlDbType.NVarChar, 30).Value = TipoCierreDiferencia;

        var resumen = new List<ResumenMovimientoCajaQuery>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            resumen.Add(new ResumenMovimientoCajaQuery(
                reader.GetString(0),
                Convert.ToInt32(reader.GetInt64(1)),
                reader.IsDBNull(2) ? 0 : reader.GetDecimal(2)));
        }

        return resumen;
    }

    public async Task<decimal> CalcularEfectivoEsperadoAsync(long idTurno, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT COALESCE(SUM(
                CASE
                    WHEN tipo_movimiento IN (N'FondoInicial', N'VentaEfectivo', N'IngresoCaja', N'AjustePositivo') THEN monto
                    WHEN tipo_movimiento IN (N'RetiroCaja', N'AjusteNegativo', N'DevolucionEfectivo') THEN -monto
                    ELSE 0
                END), 0)
            FROM dbo.movimiento_caja
            WHERE idTurno = @idTurno
              AND estado = N'Confirmado'
              AND tipo_movimiento <> N'CierreDiferencia';
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@idTurno", idTurno);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result == DBNull.Value ? 0 : Convert.ToDecimal(result);
    }

    public async Task<CierreTurnoQuery> CerrarTurnoAsync(
        long idTurno,
        decimal efectivoContado,
        string? observacion,
        byte[] rowVersion,
        int usuarioId,
        Guid idempotencyKey,
        byte[] requestHash,
        CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            if (!await UsuarioActivoExisteAsync(connection, transaction, usuarioId, cancellationToken))
            {
                throw new CajaBusinessException(CajaServiceStatus.Invalid, "Usuario invalido.");
            }

            var idempotencia = await GetCajaIdempotenciaForUpdateAsync(
                connection,
                transaction,
                usuarioId,
                CajaIdempotencyOperation.CerrarTurno.ToString(),
                idempotencyKey,
                cancellationToken);

            if (idempotencia is not null)
            {
                var existing = await ResolveExistingCierreIdempotenciaAsync(
                    connection,
                    transaction,
                    idempotencia,
                    requestHash,
                    cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                return existing;
            }

            var turno = await GetTurnoByIdForUpdateAsync(connection, transaction, idTurno, cancellationToken);
            if (turno is null)
            {
                throw new CajaBusinessException(CajaServiceStatus.NotFound, "Turno no encontrado.");
            }

            if (!turno.RowVersion.SequenceEqual(rowVersion))
            {
                throw new CajaBusinessException(CajaServiceStatus.Conflict, "Version de turno desactualizada.");
            }

            if (!string.Equals(turno.Estado, EstadoAbierto, StringComparison.Ordinal))
            {
                throw new CajaBusinessException(CajaServiceStatus.Conflict, "El turno no esta abierto.");
            }

            var idCajaIdempotencia = await CrearCajaIdempotenciaEnProcesoAsync(
                connection,
                transaction,
                usuarioId,
                idTurno,
                turno.CajaCodigo,
                CajaIdempotencyOperation.CerrarTurno.ToString(),
                idempotencyKey,
                requestHash,
                cancellationToken);

            await CambiarTurnoAEnCierreAsync(connection, transaction, idTurno, rowVersion, cancellationToken);

            var efectivoEsperado = await CalcularEfectivoEsperadoParaCierreAsync(
                connection,
                transaction,
                idTurno,
                cancellationToken);

            var diferencia = efectivoContado - efectivoEsperado;
            if (diferencia != 0 && string.IsNullOrWhiteSpace(observacion))
            {
                throw new CajaBusinessException(CajaServiceStatus.Invalid, "Observacion requerida cuando existe diferencia.");
            }

            long? idMovimientoDiferencia = null;
            if (diferencia != 0)
            {
                idMovimientoDiferencia = await CrearMovimientoCierreDiferenciaAsync(
                    connection,
                    transaction,
                    idTurno,
                    Math.Abs(diferencia),
                    observacion!,
                    usuarioId,
                    cancellationToken);
            }

            await CerrarTurnoFinalAsync(
                connection,
                transaction,
                idTurno,
                usuarioId,
                efectivoEsperado,
                efectivoContado,
                diferencia,
                observacion,
                cancellationToken);

            await CompletarCajaIdempotenciaCierreAsync(
                connection,
                transaction,
                idCajaIdempotencia,
                idTurno,
                idMovimientoDiferencia,
                ResultadoTurnoCerrado,
                cancellationToken);

            var turnoCerrado = await GetTurnoByIdAsync(connection, transaction, idTurno, cancellationToken)
                ?? throw new InvalidOperationException("Caja turn was not found after closing.");
            var resumen = await GetResumenMovimientosAsync(connection, transaction, idTurno, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return new CierreTurnoQuery(turnoCerrado, idMovimientoDiferencia is not null, resumen);
        }
        catch (SqlException exception) when (IsUniqueConstraintViolation(exception))
        {
            await RollbackQuietlyAsync(transaction, cancellationToken);
            throw new CajaBusinessException(CajaServiceStatus.Conflict, "Solicitud de caja duplicada o en conflicto.");
        }
        catch
        {
            await RollbackQuietlyAsync(transaction, cancellationToken);
            throw;
        }
    }

    private static async Task<bool> UsuarioActivoExisteAsync(
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

        await using var command = CreateCommand(connection, transaction, sql);
        command.Parameters.Add("@usuario_id", SqlDbType.Int).Value = usuarioId;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result) == 1;
    }

    private static async Task<bool> TurnoAbiertoExisteAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string cajaCodigo,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT_BIG(1)
            FROM dbo.caja_turno WITH (UPDLOCK, HOLDLOCK)
            WHERE caja_codigo = @caja_codigo
              AND estado IN (@estado_abierto, @estado_en_cierre);
            """;

        await using var command = CreateCommand(connection, transaction, sql);
        command.Parameters.Add("@caja_codigo", SqlDbType.NVarChar, 50).Value = cajaCodigo;
        command.Parameters.Add("@estado_abierto", SqlDbType.NVarChar, 20).Value = EstadoAbierto;
        command.Parameters.Add("@estado_en_cierre", SqlDbType.NVarChar, 20).Value = EstadoEnCierre;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result) > 0;
    }

    private static async Task<long?> GetTurnoAbiertoIdForUpdateAsync(
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

        await using var command = CreateCommand(connection, transaction, sql);
        command.Parameters.Add("@caja_codigo", SqlDbType.NVarChar, 50).Value = cajaCodigo;
        command.Parameters.Add("@estado_abierto", SqlDbType.NVarChar, 20).Value = EstadoAbierto;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null || result == DBNull.Value ? null : Convert.ToInt64(result);
    }

    private static async Task<CajaIdempotencyState?> GetCajaIdempotenciaForUpdateAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int usuarioId,
        string operacion,
        Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                idCajaIdempotencia,
                usuario_id,
                idTurno,
                caja_codigo,
                operacion,
                idempotency_key,
                request_hash,
                estado,
                idMovimiento,
                cierre_referencia_id,
                resultado_codigo,
                creado_utc,
                actualizado_utc,
                completado_utc,
                row_version
            FROM dbo.caja_idempotencia WITH (UPDLOCK, HOLDLOCK)
            WHERE usuario_id = @usuario_id
              AND operacion = @operacion
              AND idempotency_key = @idempotency_key;
            """;

        await using var command = CreateCommand(connection, transaction, sql);
        command.Parameters.Add("@usuario_id", SqlDbType.Int).Value = usuarioId;
        command.Parameters.Add("@operacion", SqlDbType.NVarChar, 40).Value = operacion;
        command.Parameters.Add("@idempotency_key", SqlDbType.UniqueIdentifier).Value = idempotencyKey;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapCajaIdempotencia(reader) : null;
    }

    private static async Task<MovimientoCajaQuery> ResolveExistingCajaIdempotenciaAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        CajaIdempotencyState state,
        byte[] requestHash,
        CancellationToken cancellationToken)
    {
        if (!state.RequestHash.SequenceEqual(requestHash))
        {
            throw new CajaBusinessException(CajaServiceStatus.Conflict, "Idempotency-Key ya fue usada con una solicitud distinta.");
        }

        if (string.Equals(state.Estado, CajaIdempotencyStatus.EnProceso, StringComparison.Ordinal))
        {
            throw new CajaBusinessException(CajaServiceStatus.Conflict, "Solicitud de caja en proceso. Reintente mas tarde con la misma key.");
        }

        if (string.Equals(state.Estado, CajaIdempotencyStatus.Completada, StringComparison.Ordinal) &&
            state.IdMovimiento is not null)
        {
            return await GetMovimientoByIdAsync(connection, transaction, state.IdMovimiento.Value, cancellationToken)
                ?? throw new CajaBusinessException(CajaServiceStatus.Conflict, "No fue posible recuperar el resultado completado.");
        }

        throw new CajaBusinessException(CajaServiceStatus.Conflict, "Estado de idempotencia no permite continuar.");
    }

    private static async Task<CajaTurnoQuery> ResolveExistingTurnoIdempotenciaAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        CajaIdempotencyState state,
        byte[] requestHash,
        CancellationToken cancellationToken)
    {
        if (!state.RequestHash.SequenceEqual(requestHash))
        {
            throw new CajaBusinessException(CajaServiceStatus.Conflict, "Idempotency-Key ya fue usada con una solicitud distinta.");
        }

        if (string.Equals(state.Estado, CajaIdempotencyStatus.EnProceso, StringComparison.Ordinal))
        {
            throw new CajaBusinessException(CajaServiceStatus.Conflict, "Solicitud de caja en proceso. Reintente mas tarde con la misma key.");
        }

        if (string.Equals(state.Estado, CajaIdempotencyStatus.Completada, StringComparison.Ordinal) &&
            state.IdTurno is not null)
        {
            return await GetTurnoByIdAsync(connection, transaction, state.IdTurno.Value, cancellationToken)
                ?? throw new CajaBusinessException(CajaServiceStatus.Conflict, "No fue posible recuperar el resultado completado.");
        }

        throw new CajaBusinessException(CajaServiceStatus.Conflict, "Estado de idempotencia no permite continuar.");
    }

    private static async Task<CierreTurnoQuery> ResolveExistingCierreIdempotenciaAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        CajaIdempotencyState state,
        byte[] requestHash,
        CancellationToken cancellationToken)
    {
        if (!state.RequestHash.SequenceEqual(requestHash))
        {
            throw new CajaBusinessException(CajaServiceStatus.Conflict, "Idempotency-Key ya fue usada con una solicitud distinta.");
        }

        if (string.Equals(state.Estado, CajaIdempotencyStatus.EnProceso, StringComparison.Ordinal))
        {
            throw new CajaBusinessException(CajaServiceStatus.Conflict, "Solicitud de caja en proceso. Reintente mas tarde con la misma key.");
        }

        if (string.Equals(state.Estado, CajaIdempotencyStatus.Completada, StringComparison.Ordinal) &&
            state.CierreReferenciaId is not null)
        {
            var turno = await GetTurnoByIdAsync(connection, transaction, state.CierreReferenciaId.Value, cancellationToken)
                ?? throw new CajaBusinessException(CajaServiceStatus.Conflict, "No fue posible recuperar el resultado completado.");
            var resumen = await GetResumenMovimientosAsync(connection, transaction, state.CierreReferenciaId.Value, cancellationToken);
            return new CierreTurnoQuery(turno, state.IdMovimiento is not null, resumen);
        }

        throw new CajaBusinessException(CajaServiceStatus.Conflict, "Estado de idempotencia no permite continuar.");
    }

    private static async Task<decimal> CalcularEfectivoDisponibleEnTurnoAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        long idTurno,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COALESCE(SUM(
                CASE
                    WHEN tipo_movimiento IN (N'FondoInicial', N'VentaEfectivo', N'IngresoCaja', N'AjustePositivo') THEN monto
                    WHEN tipo_movimiento IN (N'RetiroCaja', N'AjusteNegativo', N'DevolucionEfectivo') THEN -monto
                    ELSE 0
                END), 0)
            FROM dbo.movimiento_caja WITH (UPDLOCK, HOLDLOCK)
            WHERE idTurno = @idTurno
              AND estado = @estado;
            """;

        await using var command = CreateCommand(connection, transaction, sql);
        command.Parameters.Add("@idTurno", SqlDbType.BigInt).Value = idTurno;
        command.Parameters.Add("@estado", SqlDbType.NVarChar, 20).Value = EstadoConfirmado;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result == DBNull.Value ? 0 : Convert.ToDecimal(result);
    }

    private static async Task<decimal> CalcularEfectivoEsperadoParaCierreAsync(
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
              AND m.estado = @estado
              AND m.tipo_movimiento <> @tipo_cierre_diferencia;
            """;

        await using var command = CreateCommand(connection, transaction, sql);
        command.Parameters.Add("@idTurno", SqlDbType.BigInt).Value = idTurno;
        command.Parameters.Add("@estado", SqlDbType.NVarChar, 20).Value = EstadoConfirmado;
        command.Parameters.Add("@tipo_cierre_diferencia", SqlDbType.NVarChar, 30).Value = TipoCierreDiferencia;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result == DBNull.Value ? 0 : Convert.ToDecimal(result);
    }

    private static async Task<long> CrearCajaIdempotenciaEnProcesoAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int usuarioId,
        long? idTurno,
        string cajaCodigo,
        string operacion,
        Guid idempotencyKey,
        byte[] requestHash,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.caja_idempotencia (
                usuario_id,
                idTurno,
                caja_codigo,
                operacion,
                idempotency_key,
                request_hash,
                estado,
                idMovimiento,
                cierre_referencia_id,
                resultado_codigo,
                creado_utc,
                actualizado_utc,
                completado_utc,
                metadata_minima)
            VALUES (
                @usuario_id,
                @idTurno,
                @caja_codigo,
                @operacion,
                @idempotency_key,
                @request_hash,
                @estado,
                NULL,
                NULL,
                NULL,
                SYSUTCDATETIME(),
                SYSUTCDATETIME(),
                NULL,
                NULL);
            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """;

        await using var command = CreateCommand(connection, transaction, sql);
        command.Parameters.Add("@usuario_id", SqlDbType.Int).Value = usuarioId;
        command.Parameters.Add("@idTurno", SqlDbType.BigInt).Value = idTurno is null ? DBNull.Value : idTurno.Value;
        command.Parameters.Add("@caja_codigo", SqlDbType.NVarChar, 50).Value = cajaCodigo;
        command.Parameters.Add("@operacion", SqlDbType.NVarChar, 40).Value = operacion;
        command.Parameters.Add("@idempotency_key", SqlDbType.UniqueIdentifier).Value = idempotencyKey;
        command.Parameters.Add("@request_hash", SqlDbType.VarBinary, 32).Value = requestHash;
        command.Parameters.Add("@estado", SqlDbType.NVarChar, 20).Value = CajaIdempotencyStatus.EnProceso;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result);
    }

    private static async Task<CajaTurnoQuery?> GetTurnoByIdForUpdateAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        long idTurno,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                idTurno, caja_codigo, estado, usuario_apertura_id, usuario_cierre_id,
                apertura_utc, cierre_utc, fondo_inicial, efectivo_esperado,
                efectivo_contado, diferencia, observacion_apertura,
                observacion_cierre, cierre_caja_id, row_version
            FROM dbo.caja_turno WITH (UPDLOCK, HOLDLOCK)
            WHERE idTurno = @idTurno;
            """;

        await using var command = CreateCommand(connection, transaction, sql);
        command.Parameters.Add("@idTurno", SqlDbType.BigInt).Value = idTurno;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapTurno(reader) : null;
    }

    private static async Task CambiarTurnoAEnCierreAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        long idTurno,
        byte[] rowVersion,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.caja_turno
            SET estado = @estado_en_cierre,
                actualizado_utc = SYSUTCDATETIME()
            WHERE idTurno = @idTurno
              AND estado = @estado_abierto
              AND row_version = @row_version;
            """;

        await using var command = CreateCommand(connection, transaction, sql);
        command.Parameters.Add("@estado_en_cierre", SqlDbType.NVarChar, 20).Value = EstadoEnCierre;
        command.Parameters.Add("@estado_abierto", SqlDbType.NVarChar, 20).Value = EstadoAbierto;
        command.Parameters.Add("@idTurno", SqlDbType.BigInt).Value = idTurno;
        command.Parameters.Add("@row_version", SqlDbType.Timestamp).Value = rowVersion;

        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        if (affected != 1)
        {
            throw new CajaBusinessException(CajaServiceStatus.Conflict, "Version de turno desactualizada.");
        }
    }

    private static async Task<long> CrearMovimientoCierreDiferenciaAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        long idTurno,
        decimal monto,
        string observacion,
        int usuarioId,
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
                SYSUTCDATETIME(),
                @usuario_id,
                NULL,
                NULL,
                NULL,
                NULL,
                @referencia,
                @observacion,
                @estado,
                NULL,
                NEWID(),
                SYSUTCDATETIME());
            """;

        await using var command = CreateCommand(connection, transaction, sql);
        command.Parameters.Add("@idTurno", SqlDbType.BigInt).Value = idTurno;
        command.Parameters.Add("@tipo_movimiento", SqlDbType.NVarChar, 30).Value = TipoCierreDiferencia;
        command.Parameters.Add("@origen", SqlDbType.NVarChar, 30).Value = OrigenApi;
        command.Parameters.Add("@monto", SqlDbType.Decimal).Value = monto;
        command.Parameters["@monto"].Precision = 18;
        command.Parameters["@monto"].Scale = 2;
        command.Parameters.Add("@moneda", SqlDbType.Char, 3).Value = MonedaColones;
        command.Parameters.Add("@usuario_id", SqlDbType.Int).Value = usuarioId;
        command.Parameters.Add("@referencia", SqlDbType.NVarChar, 100).Value = ResultadoTurnoCerrado;
        command.Parameters.Add("@observacion", SqlDbType.NVarChar, 250).Value = observacion;
        command.Parameters.Add("@estado", SqlDbType.NVarChar, 20).Value = EstadoConfirmado;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result);
    }

    private static async Task CerrarTurnoFinalAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        long idTurno,
        int usuarioId,
        decimal efectivoEsperado,
        decimal efectivoContado,
        decimal diferencia,
        string? observacion,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.caja_turno
            SET estado = @estado_cerrado,
                usuario_cierre_id = @usuario_cierre_id,
                cierre_utc = SYSUTCDATETIME(),
                efectivo_esperado = @efectivo_esperado,
                efectivo_contado = @efectivo_contado,
                diferencia = @diferencia,
                observacion_cierre = @observacion_cierre,
                actualizado_utc = SYSUTCDATETIME()
            WHERE idTurno = @idTurno
              AND estado = @estado_en_cierre;
            """;

        await using var command = CreateCommand(connection, transaction, sql);
        command.Parameters.Add("@estado_cerrado", SqlDbType.NVarChar, 20).Value = EstadoCerrado;
        command.Parameters.Add("@estado_en_cierre", SqlDbType.NVarChar, 20).Value = EstadoEnCierre;
        command.Parameters.Add("@usuario_cierre_id", SqlDbType.Int).Value = usuarioId;
        command.Parameters.Add("@idTurno", SqlDbType.BigInt).Value = idTurno;
        command.Parameters.Add("@efectivo_esperado", SqlDbType.Decimal).Value = efectivoEsperado;
        command.Parameters["@efectivo_esperado"].Precision = 18;
        command.Parameters["@efectivo_esperado"].Scale = 2;
        command.Parameters.Add("@efectivo_contado", SqlDbType.Decimal).Value = efectivoContado;
        command.Parameters["@efectivo_contado"].Precision = 18;
        command.Parameters["@efectivo_contado"].Scale = 2;
        command.Parameters.Add("@diferencia", SqlDbType.Decimal).Value = diferencia;
        command.Parameters["@diferencia"].Precision = 18;
        command.Parameters["@diferencia"].Scale = 2;
        command.Parameters.Add("@observacion_cierre", SqlDbType.NVarChar, 250).Value =
            string.IsNullOrWhiteSpace(observacion) ? DBNull.Value : observacion.Trim();

        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        if (affected != 1)
        {
            throw new CajaBusinessException(CajaServiceStatus.Conflict, "No fue posible cerrar el turno.");
        }
    }

    private static async Task CompletarCajaIdempotenciaCierreAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        long idCajaIdempotencia,
        long idTurno,
        long? idMovimiento,
        string resultadoCodigo,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.caja_idempotencia
            SET estado = @estado,
                idTurno = @idTurno,
                idMovimiento = @idMovimiento,
                cierre_referencia_id = @cierre_referencia_id,
                resultado_codigo = @resultado_codigo,
                actualizado_utc = SYSUTCDATETIME(),
                completado_utc = SYSUTCDATETIME()
            WHERE idCajaIdempotencia = @idCajaIdempotencia
              AND estado = @estado_en_proceso;
            """;

        await using var command = CreateCommand(connection, transaction, sql);
        command.Parameters.Add("@estado", SqlDbType.NVarChar, 20).Value = CajaIdempotencyStatus.Completada;
        command.Parameters.Add("@idTurno", SqlDbType.BigInt).Value = idTurno;
        command.Parameters.Add("@idMovimiento", SqlDbType.BigInt).Value = idMovimiento is null ? DBNull.Value : idMovimiento.Value;
        command.Parameters.Add("@cierre_referencia_id", SqlDbType.BigInt).Value = idTurno;
        command.Parameters.Add("@resultado_codigo", SqlDbType.NVarChar, 80).Value = resultadoCodigo;
        command.Parameters.Add("@idCajaIdempotencia", SqlDbType.BigInt).Value = idCajaIdempotencia;
        command.Parameters.Add("@estado_en_proceso", SqlDbType.NVarChar, 20).Value = CajaIdempotencyStatus.EnProceso;

        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        if (affected != 1)
        {
            throw new CajaBusinessException(CajaServiceStatus.Conflict, "Estado de idempotencia no permite completar la operacion.");
        }
    }

    private static async Task<long> CrearTurnoAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string cajaCodigo,
        decimal fondoInicial,
        string? observacion,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            DECLARE @now datetime2(3) = SYSUTCDATETIME();

            INSERT INTO dbo.caja_turno (
                caja_codigo,
                estado,
                usuario_apertura_id,
                usuario_cierre_id,
                apertura_utc,
                cierre_utc,
                fondo_inicial,
                efectivo_esperado,
                efectivo_contado,
                diferencia,
                observacion_apertura,
                observacion_cierre,
                cierre_caja_id,
                creado_utc,
                actualizado_utc)
            OUTPUT INSERTED.idTurno
            VALUES (
                @caja_codigo,
                @estado,
                @usuario_apertura_id,
                NULL,
                @now,
                NULL,
                @fondo_inicial,
                NULL,
                NULL,
                NULL,
                @observacion_apertura,
                NULL,
                NULL,
                @now,
                @now);
            """;

        await using var command = CreateCommand(connection, transaction, sql);
        command.Parameters.Add("@caja_codigo", SqlDbType.NVarChar, 50).Value = cajaCodigo;
        command.Parameters.Add("@estado", SqlDbType.NVarChar, 20).Value = EstadoAbierto;
        command.Parameters.Add("@usuario_apertura_id", SqlDbType.Int).Value = usuarioId;
        command.Parameters.Add("@fondo_inicial", SqlDbType.Decimal).Value = fondoInicial;
        command.Parameters["@fondo_inicial"].Precision = 18;
        command.Parameters["@fondo_inicial"].Scale = 2;
        command.Parameters.Add("@observacion_apertura", SqlDbType.NVarChar, 250).Value =
            string.IsNullOrWhiteSpace(observacion) ? DBNull.Value : observacion;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result);
    }

    private static async Task CompletarCajaIdempotenciaAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        long idCajaIdempotencia,
        long idTurno,
        long idMovimiento,
        string resultadoCodigo,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.caja_idempotencia
            SET estado = @estado,
                idTurno = @idTurno,
                idMovimiento = @idMovimiento,
                resultado_codigo = @resultado_codigo,
                actualizado_utc = SYSUTCDATETIME(),
                completado_utc = SYSUTCDATETIME()
            WHERE idCajaIdempotencia = @idCajaIdempotencia
              AND estado = @estado_en_proceso;
            """;

        await using var command = CreateCommand(connection, transaction, sql);
        command.Parameters.Add("@estado", SqlDbType.NVarChar, 20).Value = CajaIdempotencyStatus.Completada;
        command.Parameters.Add("@idTurno", SqlDbType.BigInt).Value = idTurno;
        command.Parameters.Add("@idMovimiento", SqlDbType.BigInt).Value = idMovimiento;
        command.Parameters.Add("@resultado_codigo", SqlDbType.NVarChar, 80).Value = resultadoCodigo;
        command.Parameters.Add("@idCajaIdempotencia", SqlDbType.BigInt).Value = idCajaIdempotencia;
        command.Parameters.Add("@estado_en_proceso", SqlDbType.NVarChar, 20).Value = CajaIdempotencyStatus.EnProceso;

        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        if (affected != 1)
        {
            throw new CajaBusinessException(CajaServiceStatus.Conflict, "Estado de idempotencia no permite completar la operacion.");
        }
    }

    private static async Task<long> CrearMovimientoFondoInicialAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        long idTurno,
        string cajaCodigo,
        decimal fondoInicial,
        string? observacion,
        int usuarioId,
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
            SELECT
                @idTurno,
                @tipo_movimiento,
                @origen,
                @monto,
                @moneda,
                apertura_utc,
                @usuario_id,
                NULL,
                NULL,
                NULL,
                NULL,
                @referencia,
                @observacion,
                @estado,
                NULL,
                NEWID(),
                apertura_utc
            FROM dbo.caja_turno
            WHERE idTurno = @idTurno;
            """;

        await using var command = CreateCommand(connection, transaction, sql);
        command.Parameters.Add("@idTurno", SqlDbType.BigInt).Value = idTurno;
        command.Parameters.Add("@tipo_movimiento", SqlDbType.NVarChar, 30).Value = TipoFondoInicial;
        command.Parameters.Add("@origen", SqlDbType.NVarChar, 30).Value = OrigenApi;
        command.Parameters.Add("@monto", SqlDbType.Decimal).Value = fondoInicial;
        command.Parameters["@monto"].Precision = 18;
        command.Parameters["@monto"].Scale = 2;
        command.Parameters.Add("@moneda", SqlDbType.Char, 3).Value = MonedaColones;
        command.Parameters.Add("@usuario_id", SqlDbType.Int).Value = usuarioId;
        command.Parameters.Add("@referencia", SqlDbType.NVarChar, 100).Value = cajaCodigo;
        command.Parameters.Add("@observacion", SqlDbType.NVarChar, 250).Value =
            string.IsNullOrWhiteSpace(observacion) ? DBNull.Value : observacion;
        command.Parameters.Add("@estado", SqlDbType.NVarChar, 20).Value = EstadoConfirmado;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null || result == DBNull.Value)
        {
            throw new InvalidOperationException("Initial cash movement was not created.");
        }

        return Convert.ToInt64(result);
    }

    private static async Task<long> CrearMovimientoIngresoCajaAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        long idTurno,
        decimal monto,
        string motivo,
        string? referencia,
        int usuarioId,
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
                SYSUTCDATETIME(),
                @usuario_id,
                NULL,
                NULL,
                NULL,
                NULL,
                @referencia,
                @observacion,
                @estado,
                NULL,
                NEWID(),
                SYSUTCDATETIME());
            """;

        await using var command = CreateCommand(connection, transaction, sql);
        command.Parameters.Add("@idTurno", SqlDbType.BigInt).Value = idTurno;
        command.Parameters.Add("@tipo_movimiento", SqlDbType.NVarChar, 30).Value = TipoIngresoCaja;
        command.Parameters.Add("@origen", SqlDbType.NVarChar, 30).Value = OrigenApi;
        command.Parameters.Add("@monto", SqlDbType.Decimal).Value = monto;
        command.Parameters["@monto"].Precision = 18;
        command.Parameters["@monto"].Scale = 2;
        command.Parameters.Add("@moneda", SqlDbType.Char, 3).Value = MonedaColones;
        command.Parameters.Add("@usuario_id", SqlDbType.Int).Value = usuarioId;
        command.Parameters.Add("@referencia", SqlDbType.NVarChar, 100).Value =
            string.IsNullOrWhiteSpace(referencia) ? DBNull.Value : referencia;
        command.Parameters.Add("@observacion", SqlDbType.NVarChar, 250).Value = motivo;
        command.Parameters.Add("@estado", SqlDbType.NVarChar, 20).Value = EstadoConfirmado;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result);
    }

    private static async Task<long> CrearMovimientoRetiroCajaAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        long idTurno,
        decimal monto,
        string motivo,
        string? referencia,
        int usuarioId,
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
                SYSUTCDATETIME(),
                @usuario_id,
                NULL,
                NULL,
                NULL,
                NULL,
                @referencia,
                @observacion,
                @estado,
                NULL,
                NEWID(),
                SYSUTCDATETIME());
            """;

        await using var command = CreateCommand(connection, transaction, sql);
        command.Parameters.Add("@idTurno", SqlDbType.BigInt).Value = idTurno;
        command.Parameters.Add("@tipo_movimiento", SqlDbType.NVarChar, 30).Value = TipoRetiroCaja;
        command.Parameters.Add("@origen", SqlDbType.NVarChar, 30).Value = OrigenApi;
        command.Parameters.Add("@monto", SqlDbType.Decimal).Value = monto;
        command.Parameters["@monto"].Precision = 18;
        command.Parameters["@monto"].Scale = 2;
        command.Parameters.Add("@moneda", SqlDbType.Char, 3).Value = MonedaColones;
        command.Parameters.Add("@usuario_id", SqlDbType.Int).Value = usuarioId;
        command.Parameters.Add("@referencia", SqlDbType.NVarChar, 100).Value =
            string.IsNullOrWhiteSpace(referencia) ? DBNull.Value : referencia;
        command.Parameters.Add("@observacion", SqlDbType.NVarChar, 250).Value = motivo;
        command.Parameters.Add("@estado", SqlDbType.NVarChar, 20).Value = EstadoConfirmado;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result);
    }

    private static async Task<CajaTurnoQuery?> GetTurnoByIdAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        long idTurno,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                idTurno, caja_codigo, estado, usuario_apertura_id, usuario_cierre_id,
                apertura_utc, cierre_utc, fondo_inicial, efectivo_esperado,
                efectivo_contado, diferencia, observacion_apertura,
                observacion_cierre, cierre_caja_id, row_version
            FROM dbo.caja_turno
            WHERE idTurno = @idTurno;
            """;

        await using var command = CreateCommand(connection, transaction, sql);
        command.Parameters.Add("@idTurno", SqlDbType.BigInt).Value = idTurno;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapTurno(reader) : null;
    }

    private static async Task<MovimientoCajaQuery?> GetMovimientoByIdAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        long idMovimiento,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                idMovimiento, idTurno, tipo_movimiento, origen, monto, moneda,
                fecha_hora_utc, usuario_id, factura, pago_id, ingreso_caja_id,
                retiro_caja_id, referencia, observacion, estado,
                reversa_de_movimiento_id
            FROM dbo.movimiento_caja
            WHERE idMovimiento = @idMovimiento;
            """;

        await using var command = CreateCommand(connection, transaction, sql);
        command.Parameters.Add("@idMovimiento", SqlDbType.BigInt).Value = idMovimiento;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapMovimiento(reader) : null;
    }

    private static SqlCommand CreateCommand(SqlConnection connection, SqlTransaction transaction, string commandText)
    {
        return new SqlCommand(commandText, connection, transaction);
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

    private static async Task RollbackQuietlyAsync(SqlTransaction transaction, CancellationToken cancellationToken)
    {
        try
        {
            await transaction.RollbackAsync(cancellationToken);
        }
        catch
        {
        }
    }

    private static CajaTurnoQuery MapTurno(SqlDataReader reader)
    {
        return new CajaTurnoQuery(
            reader.GetInt64(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetInt32(3),
            reader.IsDBNull(4) ? null : reader.GetInt32(4),
            reader.GetDateTime(5),
            reader.IsDBNull(6) ? null : reader.GetDateTime(6),
            reader.GetDecimal(7),
            reader.IsDBNull(8) ? null : reader.GetDecimal(8),
            reader.IsDBNull(9) ? null : reader.GetDecimal(9),
            reader.IsDBNull(10) ? null : reader.GetDecimal(10),
            reader.IsDBNull(11) ? null : reader.GetString(11),
            reader.IsDBNull(12) ? null : reader.GetString(12),
            reader.IsDBNull(13) ? null : reader.GetInt32(13),
            (byte[])reader[14]);
    }

    private static MovimientoCajaQuery MapMovimiento(SqlDataReader reader)
    {
        return new MovimientoCajaQuery(
            reader.GetInt64(0),
            reader.GetInt64(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetDecimal(4),
            reader.GetString(5),
            reader.GetDateTime(6),
            reader.GetInt32(7),
            reader.IsDBNull(8) ? null : reader.GetInt32(8),
            reader.IsDBNull(9) ? null : reader.GetInt64(9),
            reader.IsDBNull(10) ? null : reader.GetInt32(10),
            reader.IsDBNull(11) ? null : reader.GetInt32(11),
            reader.IsDBNull(12) ? null : reader.GetString(12),
            reader.IsDBNull(13) ? null : reader.GetString(13),
            reader.GetString(14),
            reader.IsDBNull(15) ? null : reader.GetInt64(15));
    }

    private static CajaIdempotencyState MapCajaIdempotencia(SqlDataReader reader)
    {
        return new CajaIdempotencyState(
            reader.GetInt64(0),
            reader.GetInt32(1),
            reader.IsDBNull(2) ? null : reader.GetInt64(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            Enum.Parse<CajaIdempotencyOperation>(reader.GetString(4), ignoreCase: false),
            reader.GetGuid(5),
            (byte[])reader[6],
            reader.GetString(7),
            reader.IsDBNull(8) ? null : reader.GetInt64(8),
            reader.IsDBNull(9) ? null : reader.GetInt64(9),
            reader.IsDBNull(10) ? null : reader.GetString(10),
            new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(11), DateTimeKind.Utc)),
            new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(12), DateTimeKind.Utc)),
            reader.IsDBNull(13) ? null : new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(13), DateTimeKind.Utc)),
            (byte[])reader[14]);
    }
}
