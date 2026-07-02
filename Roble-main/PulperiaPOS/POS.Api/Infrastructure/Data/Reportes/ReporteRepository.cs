using System.Data;
using Microsoft.Data.SqlClient;
using POS.Api.Application.Reportes;
using POS.Api.Contracts.Reportes;

namespace POS.Api.Infrastructure.Data.Reportes;

public sealed class ReporteRepository : IReporteRepository
{
    private readonly IDatabaseConnectionFactory connectionFactory;

    public ReporteRepository(IDatabaseConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<ReporteVentasResumenResponse> GetVentasResumenAsync(DateTime? desdeUtc, DateTime? hastaUtc, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var totals = await QuerySingleAsync(connection, @"
WITH ventas_base AS (
    SELECT
        v.factura,
        CAST(ISNULL(v.total, 0) AS decimal(18,2)) AS total,
        CASE WHEN vr.idReversa IS NULL THEN 0 ELSE 1 END AS reversada
    FROM dbo.ventas v
    LEFT JOIN dbo.venta_reversa vr ON vr.factura = v.factura AND vr.estado = N'Confirmada'
    WHERE (@desdeUtc IS NULL OR TRY_CONVERT(datetime2, v.fecha) >= @desdeUtc)
      AND (@hastaUtc IS NULL OR TRY_CONVERT(datetime2, v.fecha) < @hastaUtc)
),
movimientos AS (
    SELECT
        SUM(CASE WHEN tipo_movimiento = N'VentaEfectivo' AND estado = N'Confirmado' THEN monto ELSE 0 END) AS efectivo_bruto,
        SUM(CASE WHEN tipo_movimiento = N'Reversa' AND estado = N'Confirmado' THEN monto ELSE 0 END) AS reversas_efectivo
    FROM dbo.movimiento_caja
    WHERE (@desdeUtc IS NULL OR fecha_hora_utc >= @desdeUtc)
      AND (@hastaUtc IS NULL OR fecha_hora_utc < @hastaUtc)
)
SELECT
    CAST(ISNULL(SUM(v.total), 0) AS decimal(18,2)) AS ventas_brutas,
    CAST(ISNULL(SUM(CASE WHEN v.reversada = 1 THEN v.total ELSE 0 END), 0) AS decimal(18,2)) AS monto_reversado,
    CAST(ISNULL(SUM(CASE WHEN v.reversada = 1 THEN 0 ELSE v.total END), 0) AS decimal(18,2)) AS ventas_netas,
    COUNT_BIG(1) AS cantidad_ventas,
    SUM(CASE WHEN v.reversada = 1 THEN 1 ELSE 0 END) AS cantidad_ventas_reversadas,
    (SELECT COUNT_BIG(1) FROM dbo.venta_reversa WHERE estado = N'Confirmada' AND (@desdeUtc IS NULL OR fecha_hora_utc >= @desdeUtc) AND (@hastaUtc IS NULL OR fecha_hora_utc < @hastaUtc)) AS cantidad_reversas,
    CAST(ISNULL((SELECT efectivo_bruto FROM movimientos), 0) AS decimal(18,2)) AS efectivo_bruto,
    CAST(ISNULL((SELECT reversas_efectivo FROM movimientos), 0) AS decimal(18,2)) AS reversas_efectivo
FROM ventas_base v;", AddDateParameters, desdeUtc, hastaUtc, cancellationToken);

        var bruto = GetDecimal(totals, "ventas_brutas");
        var reversado = GetDecimal(totals, "monto_reversado");
        var efectivoBruto = GetDecimal(totals, "efectivo_bruto");
        var reversasEfectivo = GetDecimal(totals, "reversas_efectivo");

        var porMetodo = await QueryRowsAsync(connection, @"
SELECT
    ISNULL(v.metodo_pago, N'Sin metodo') AS metodo_pago,
    CAST(ISNULL(SUM(ISNULL(v.total, 0)), 0) AS decimal(18,2)) AS bruto,
    CAST(ISNULL(SUM(CASE WHEN vr.idReversa IS NULL THEN 0 ELSE ISNULL(v.total, 0) END), 0) AS decimal(18,2)) AS reversado,
    CAST(ISNULL(SUM(CASE WHEN vr.idReversa IS NULL THEN ISNULL(v.total, 0) ELSE 0 END), 0) AS decimal(18,2)) AS neto,
    COUNT_BIG(1) AS cantidad
FROM dbo.ventas v
LEFT JOIN dbo.venta_reversa vr ON vr.factura = v.factura AND vr.estado = N'Confirmada'
WHERE (@desdeUtc IS NULL OR TRY_CONVERT(datetime2, v.fecha) >= @desdeUtc)
  AND (@hastaUtc IS NULL OR TRY_CONVERT(datetime2, v.fecha) < @hastaUtc)
GROUP BY ISNULL(v.metodo_pago, N'Sin metodo')
ORDER BY metodo_pago;", AddDateParameters, desdeUtc, hastaUtc, cancellationToken);

        var productos = await QueryRowsAsync(connection, @"
SELECT TOP (25)
    i.nombre AS producto,
    CAST(SUM(d.cantidad) AS decimal(18,2)) AS cantidad_bruta,
    CAST(SUM(CASE WHEN vr.idReversa IS NULL THEN 0 ELSE d.cantidad END) AS decimal(18,2)) AS cantidad_restaurada,
    CAST(SUM(CASE WHEN vr.idReversa IS NULL THEN d.cantidad ELSE 0 END) AS decimal(18,2)) AS cantidad_neta,
    CAST(SUM(CASE WHEN vr.idReversa IS NULL THEN d.cantidad * d.precio_unitario ELSE 0 END) AS decimal(18,2)) AS venta_neta
FROM dbo.DetalleVenta d
JOIN dbo.ventas v ON v.factura = d.factura
JOIN dbo.inventario i ON i.idProducto = d.producto_id
LEFT JOIN dbo.venta_reversa vr ON vr.factura = v.factura AND vr.estado = N'Confirmada'
WHERE (@desdeUtc IS NULL OR TRY_CONVERT(datetime2, v.fecha) >= @desdeUtc)
  AND (@hastaUtc IS NULL OR TRY_CONVERT(datetime2, v.fecha) < @hastaUtc)
GROUP BY i.nombre
ORDER BY venta_neta DESC, producto ASC;", AddDateParameters, desdeUtc, hastaUtc, cancellationToken);

        return new ReporteVentasResumenResponse(
            bruto,
            reversado,
            bruto - reversado,
            GetInt32(totals, "cantidad_ventas"),
            GetInt32(totals, "cantidad_ventas_reversadas"),
            GetInt32(totals, "cantidad_reversas"),
            efectivoBruto,
            reversasEfectivo,
            efectivoBruto - reversasEfectivo,
            porMetodo.Select(row => new ReporteMetodoPagoTotalResponse(
                GetString(row, "metodo_pago"),
                GetDecimal(row, "bruto"),
                GetDecimal(row, "reversado"),
                GetDecimal(row, "neto"),
                GetInt32(row, "cantidad"))).ToArray(),
            productos.Select(row => new ReporteProductoNetoResponse(
                GetString(row, "producto"),
                GetDecimal(row, "cantidad_bruta"),
                GetDecimal(row, "cantidad_restaurada"),
                GetDecimal(row, "cantidad_neta"),
                GetDecimal(row, "venta_neta"))).ToArray());
    }

    public async Task<IReadOnlyCollection<ReporteVentaDetalleResponse>> GetVentasDetalleAsync(DateTime? desdeUtc, DateTime? hastaUtc, int limit, int offset, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var rows = await QueryRowsAsync(connection, @"
SELECT
    v.factura,
    TRY_CONVERT(datetime2, v.fecha) AS fecha,
    CASE WHEN vr.idReversa IS NULL THEN N'Confirmada' ELSE N'Reversada' END AS estado,
    CAST(ISNULL(v.total, 0) AS decimal(18,2)) AS total,
    ISNULL(v.metodo_pago, N'Sin metodo') AS metodo_pago,
    CASE WHEN vp.idPago IS NULL THEN N'Historico SQL' ELSE N'Venta API' END AS origen,
    CASE WHEN vr.idReversa IS NULL THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS reversada,
    vr.fecha_hora_utc AS reversa_utc,
    CAST(CASE WHEN vr.idReversa IS NULL THEN ISNULL(v.total, 0) ELSE 0 END AS decimal(18,2)) AS impacto_neto
FROM dbo.ventas v
LEFT JOIN dbo.venta_reversa vr ON vr.factura = v.factura AND vr.estado = N'Confirmada'
OUTER APPLY (
    SELECT TOP (1) idPago
    FROM dbo.venta_pago
    WHERE factura = v.factura
    ORDER BY idPago DESC
) vp
WHERE (@desdeUtc IS NULL OR TRY_CONVERT(datetime2, v.fecha) >= @desdeUtc)
  AND (@hastaUtc IS NULL OR TRY_CONVERT(datetime2, v.fecha) < @hastaUtc)
ORDER BY v.factura DESC
OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;", command =>
        {
            AddDateParameters(command, desdeUtc, hastaUtc);
            command.Parameters.Add("@offset", SqlDbType.Int).Value = offset;
            command.Parameters.Add("@limit", SqlDbType.Int).Value = limit;
        }, cancellationToken);

        return rows.Select(row => new ReporteVentaDetalleResponse(
            GetInt32(row, "factura"),
            GetNullableDateTime(row, "fecha"),
            GetString(row, "estado"),
            GetDecimal(row, "total"),
            GetString(row, "metodo_pago"),
            GetString(row, "origen"),
            GetBoolean(row, "reversada"),
            GetNullableDateTime(row, "reversa_utc"),
            GetDecimal(row, "impacto_neto"))).ToArray();
    }

    public async Task<IReadOnlyCollection<ReporteReversaResponse>> GetReversasAsync(DateTime? desdeUtc, DateTime? hastaUtc, int limit, int offset, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var rows = await QueryRowsAsync(connection, @"
SELECT
    vr.factura,
    CAST(vr.monto AS decimal(18,2)) AS monto,
    vr.moneda,
    vr.fecha_hora_utc,
    vr.motivo,
    vr.estado,
    CASE WHEN mc.idMovimiento IS NULL THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS tiene_movimiento,
    CASE WHEN mc.idMovimiento IS NOT NULL AND mc.tipo_movimiento = N'Reversa' AND mc.estado = N'Confirmado' THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS consistente
FROM dbo.venta_reversa vr
LEFT JOIN dbo.movimiento_caja mc ON mc.idMovimiento = vr.idMovimientoCompensatorio
WHERE (@desdeUtc IS NULL OR vr.fecha_hora_utc >= @desdeUtc)
  AND (@hastaUtc IS NULL OR vr.fecha_hora_utc < @hastaUtc)
ORDER BY vr.fecha_hora_utc DESC
OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;", command =>
        {
            AddDateParameters(command, desdeUtc, hastaUtc);
            command.Parameters.Add("@offset", SqlDbType.Int).Value = offset;
            command.Parameters.Add("@limit", SqlDbType.Int).Value = limit;
        }, cancellationToken);

        return rows.Select(row => new ReporteReversaResponse(
            GetInt32(row, "factura"),
            GetDecimal(row, "monto"),
            GetString(row, "moneda"),
            GetDateTime(row, "fecha_hora_utc"),
            GetString(row, "motivo"),
            GetString(row, "estado"),
            GetBoolean(row, "tiene_movimiento"),
            GetBoolean(row, "consistente"))).ToArray();
    }

    public async Task<ReporteCajaResumenResponse> GetCajaResumenAsync(DateTime? desdeUtc, DateTime? hastaUtc, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var row = await QuerySingleAsync(connection, @"
SELECT
    (SELECT COUNT_BIG(1) FROM dbo.caja_turno WHERE estado = N'Abierto' AND (@desdeUtc IS NULL OR apertura_utc >= @desdeUtc) AND (@hastaUtc IS NULL OR apertura_utc < @hastaUtc)) AS turnos_abiertos,
    (SELECT COUNT_BIG(1) FROM dbo.caja_turno WHERE estado = N'EnCierre' AND (@desdeUtc IS NULL OR apertura_utc >= @desdeUtc) AND (@hastaUtc IS NULL OR apertura_utc < @hastaUtc)) AS turnos_en_cierre,
    (SELECT COUNT_BIG(1) FROM dbo.caja_turno WHERE estado = N'Cerrado' AND (@desdeUtc IS NULL OR apertura_utc >= @desdeUtc) AND (@hastaUtc IS NULL OR apertura_utc < @hastaUtc)) AS turnos_cerrados,
    CAST(ISNULL(SUM(CASE WHEN tipo_movimiento = N'FondoInicial' THEN monto ELSE 0 END), 0) AS decimal(18,2)) AS fondo_inicial,
    CAST(ISNULL(SUM(CASE WHEN tipo_movimiento = N'IngresoCaja' THEN monto ELSE 0 END), 0) AS decimal(18,2)) AS ingresos,
    CAST(ISNULL(SUM(CASE WHEN tipo_movimiento = N'RetiroCaja' THEN monto ELSE 0 END), 0) AS decimal(18,2)) AS retiros,
    CAST(ISNULL(SUM(CASE WHEN tipo_movimiento = N'VentaEfectivo' THEN monto ELSE 0 END), 0) AS decimal(18,2)) AS venta_efectivo,
    CAST(ISNULL(SUM(CASE WHEN tipo_movimiento = N'Reversa' THEN monto ELSE 0 END), 0) AS decimal(18,2)) AS reversas,
    CAST(ISNULL(SUM(CASE WHEN tipo_movimiento = N'CierreDiferencia' THEN monto ELSE 0 END), 0) AS decimal(18,2)) AS cierre_diferencia,
    CAST(ISNULL((SELECT SUM(ISNULL(efectivo_contado, 0)) FROM dbo.caja_turno WHERE estado = N'Cerrado' AND (@desdeUtc IS NULL OR apertura_utc >= @desdeUtc) AND (@hastaUtc IS NULL OR apertura_utc < @hastaUtc)), 0) AS decimal(18,2)) AS efectivo_contado,
    CAST(ISNULL((SELECT SUM(ISNULL(diferencia, 0)) FROM dbo.caja_turno WHERE estado = N'Cerrado' AND (@desdeUtc IS NULL OR apertura_utc >= @desdeUtc) AND (@hastaUtc IS NULL OR apertura_utc < @hastaUtc)), 0) AS decimal(18,2)) AS diferencia
FROM dbo.movimiento_caja
WHERE estado = N'Confirmado'
  AND (@desdeUtc IS NULL OR fecha_hora_utc >= @desdeUtc)
  AND (@hastaUtc IS NULL OR fecha_hora_utc < @hastaUtc);", AddDateParameters, desdeUtc, hastaUtc, cancellationToken);

        var fondo = GetDecimal(row, "fondo_inicial");
        var ingresos = GetDecimal(row, "ingresos");
        var retiros = GetDecimal(row, "retiros");
        var ventaEfectivo = GetDecimal(row, "venta_efectivo");
        var reversas = GetDecimal(row, "reversas");

        return new ReporteCajaResumenResponse(
            GetInt32(row, "turnos_abiertos"),
            GetInt32(row, "turnos_en_cierre"),
            GetInt32(row, "turnos_cerrados"),
            fondo,
            ingresos,
            retiros,
            ventaEfectivo,
            reversas,
            GetDecimal(row, "cierre_diferencia"),
            fondo + ingresos + ventaEfectivo - retiros - reversas,
            GetDecimal(row, "efectivo_contado"),
            GetDecimal(row, "diferencia"),
            "Caja API");
    }

    public async Task<IReadOnlyCollection<ReporteTurnoCajaResponse>> GetTurnosAsync(DateTime? desdeUtc, DateTime? hastaUtc, int limit, int offset, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var rows = await QueryRowsAsync(connection, @"
SELECT caja_codigo, estado, apertura_utc, cierre_utc,
       CAST(fondo_inicial AS decimal(18,2)) AS fondo_inicial,
       CAST(efectivo_esperado AS decimal(18,2)) AS efectivo_esperado,
       CAST(efectivo_contado AS decimal(18,2)) AS efectivo_contado,
       CAST(diferencia AS decimal(18,2)) AS diferencia
FROM dbo.caja_turno
WHERE (@desdeUtc IS NULL OR apertura_utc >= @desdeUtc)
  AND (@hastaUtc IS NULL OR apertura_utc < @hastaUtc)
ORDER BY apertura_utc DESC
OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;", command =>
        {
            AddDateParameters(command, desdeUtc, hastaUtc);
            command.Parameters.Add("@offset", SqlDbType.Int).Value = offset;
            command.Parameters.Add("@limit", SqlDbType.Int).Value = limit;
        }, cancellationToken);

        return rows.Select(row => new ReporteTurnoCajaResponse(
            GetString(row, "caja_codigo"),
            GetString(row, "estado"),
            GetDateTime(row, "apertura_utc"),
            GetNullableDateTime(row, "cierre_utc"),
            GetDecimal(row, "fondo_inicial"),
            GetDecimal(row, "efectivo_esperado"),
            GetNullableDecimal(row, "efectivo_contado"),
            GetNullableDecimal(row, "diferencia"),
            "Caja API")).ToArray();
    }

    public async Task<IReadOnlyCollection<ReporteMovimientoCajaResponse>> GetMovimientosAsync(DateTime? desdeUtc, DateTime? hastaUtc, int limit, int offset, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var rows = await QueryRowsAsync(connection, @"
SELECT tipo_movimiento, estado, CAST(monto AS decimal(18,2)) AS monto, moneda, fecha_hora_utc, origen, factura
FROM dbo.movimiento_caja
WHERE (@desdeUtc IS NULL OR fecha_hora_utc >= @desdeUtc)
  AND (@hastaUtc IS NULL OR fecha_hora_utc < @hastaUtc)
ORDER BY fecha_hora_utc DESC
OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;", command =>
        {
            AddDateParameters(command, desdeUtc, hastaUtc);
            command.Parameters.Add("@offset", SqlDbType.Int).Value = offset;
            command.Parameters.Add("@limit", SqlDbType.Int).Value = limit;
        }, cancellationToken);

        return rows.Select(row => new ReporteMovimientoCajaResponse(
            GetString(row, "tipo_movimiento"),
            GetString(row, "estado"),
            GetDecimal(row, "monto"),
            GetString(row, "moneda"),
            GetDateTime(row, "fecha_hora_utc"),
            GetString(row, "origen"),
            GetNullableInt32(row, "factura"))).ToArray();
    }

    public async Task<IReadOnlyCollection<ReporteInconsistenciaResponse>> GetInconsistenciasAsync(CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var rows = await QueryRowsAsync(connection, @"
SELECT N'VENTA_EFECTIVO_SIN_MOVIMIENTO' AS codigo, N'Alta' AS severidad,
       N'Venta efectiva con pago API registrado sin movimiento VentaEfectivo confirmado.' AS descripcion,
       COUNT_BIG(1) AS cantidad,
       N'Revisar idempotencia de venta y movimiento de caja asociado.' AS accion
FROM dbo.venta_pago p
WHERE p.metodo_pago = N'Efectivo'
  AND p.estado = N'Registrado'
  AND NOT EXISTS (
      SELECT 1 FROM dbo.movimiento_caja m
      WHERE m.pago_id = p.idPago
        AND m.tipo_movimiento = N'VentaEfectivo'
        AND m.estado = N'Confirmado')
UNION ALL
SELECT N'MOVIMIENTO_VENTA_SIN_PAGO', N'Alta',
       N'Movimiento VentaEfectivo sin pago registrado valido.',
       COUNT_BIG(1),
       N'Revisar relacion entre movimiento, pago y venta.'
FROM dbo.movimiento_caja m
WHERE m.tipo_movimiento = N'VentaEfectivo'
  AND m.estado = N'Confirmado'
  AND NOT EXISTS (
      SELECT 1 FROM dbo.venta_pago p
      WHERE p.idPago = m.pago_id
        AND p.estado = N'Registrado')
UNION ALL
SELECT N'REVERSA_SIN_MOVIMIENTO', N'Alta',
       N'Reversa confirmada sin movimiento compensatorio valido.',
       COUNT_BIG(1),
       N'Revisar integridad entre venta_reversa y movimiento_caja.'
FROM dbo.venta_reversa vr
WHERE vr.estado = N'Confirmada'
  AND NOT EXISTS (
      SELECT 1 FROM dbo.movimiento_caja m
      WHERE m.idMovimiento = vr.idMovimientoCompensatorio
        AND m.tipo_movimiento = N'Reversa'
        AND m.estado = N'Confirmado')
UNION ALL
SELECT N'MOVIMIENTO_REVERSA_HUERFANO', N'Alta',
       N'Movimiento Reversa sin registro venta_reversa valido.',
       COUNT_BIG(1),
       N'Revisar movimiento compensatorio y reversa asociada.'
FROM dbo.movimiento_caja m
WHERE m.tipo_movimiento = N'Reversa'
  AND m.estado = N'Confirmado'
  AND NOT EXISTS (
      SELECT 1 FROM dbo.venta_reversa vr
      WHERE vr.idMovimientoCompensatorio = m.idMovimiento)
UNION ALL
SELECT N'IDEMPOTENCIA_VENTA_PENDIENTE', N'Media',
       N'Idempotencia de venta o reversa en proceso.',
       COUNT_BIG(1),
       N'Validar si la operacion quedo pendiente o debe reintentarse con la misma intencion.'
FROM dbo.venta_idempotencia
WHERE estado = N'EnProceso'
UNION ALL
SELECT N'IDEMPOTENCIA_CAJA_PENDIENTE', N'Media',
       N'Idempotencia de caja en proceso.',
       COUNT_BIG(1),
       N'Validar estado de caja antes de ejecutar nuevas operaciones.'
FROM dbo.caja_idempotencia
WHERE estado = N'EnProceso'
UNION ALL
SELECT N'DOBLE_REVERSA_VENTA', N'Alta',
       N'Mas de una reversa confirmada para una misma venta.',
       COUNT_BIG(1),
       N'No corregir automaticamente; revisar restriccion e historial.'
FROM (
    SELECT factura
    FROM dbo.venta_reversa
    WHERE estado = N'Confirmada'
    GROUP BY factura
    HAVING COUNT_BIG(1) > 1
) duplicadas
UNION ALL
SELECT N'DOBLE_MOVIMIENTO_PAGO', N'Alta',
       N'Mas de un movimiento VentaEfectivo confirmado para un mismo pago.',
       COUNT_BIG(1),
       N'Revisar idempotencia y relacion pago-movimiento.'
FROM (
    SELECT pago_id
    FROM dbo.movimiento_caja
    WHERE tipo_movimiento = N'VentaEfectivo'
      AND estado = N'Confirmado'
      AND pago_id IS NOT NULL
    GROUP BY pago_id
    HAVING COUNT_BIG(1) > 1
) duplicados;", null, cancellationToken);

        return rows.Select(row => new ReporteInconsistenciaResponse(
            GetString(row, "codigo"),
            GetString(row, "severidad"),
            GetString(row, "descripcion"),
            GetInt32(row, "cantidad"),
            GetString(row, "accion"))).ToArray();
    }

    private static async Task<Dictionary<string, object?>> QuerySingleAsync(
        SqlConnection connection,
        string commandText,
        Action<SqlCommand, DateTime?, DateTime?> parameterize,
        DateTime? desdeUtc,
        DateTime? hastaUtc,
        CancellationToken cancellationToken)
    {
        var rows = await QueryRowsAsync(connection, commandText, command => parameterize(command, desdeUtc, hastaUtc), cancellationToken);
        return rows.FirstOrDefault() ?? [];
    }

    private static async Task<IReadOnlyCollection<Dictionary<string, object?>>> QueryRowsAsync(
        SqlConnection connection,
        string commandText,
        Action<SqlCommand, DateTime?, DateTime?> parameterize,
        DateTime? desdeUtc,
        DateTime? hastaUtc,
        CancellationToken cancellationToken)
    {
        return await QueryRowsAsync(connection, commandText, command => parameterize(command, desdeUtc, hastaUtc), cancellationToken);
    }

    private static async Task<IReadOnlyCollection<Dictionary<string, object?>>> QueryRowsAsync(
        SqlConnection connection,
        string commandText,
        Action<SqlCommand>? parameterize,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        command.CommandTimeout = 30;
        parameterize?.Invoke(command);

        var rows = new List<Dictionary<string, object?>>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            rows.Add(row);
        }

        return rows;
    }

    private static void AddDateParameters(SqlCommand command, DateTime? desdeUtc, DateTime? hastaUtc)
    {
        command.Parameters.Add("@desdeUtc", SqlDbType.DateTime2).Value = desdeUtc is null ? DBNull.Value : desdeUtc.Value;
        command.Parameters.Add("@hastaUtc", SqlDbType.DateTime2).Value = hastaUtc is null ? DBNull.Value : hastaUtc.Value;
    }

    private static string GetString(IReadOnlyDictionary<string, object?> row, string name)
    {
        return Convert.ToString(row[name]) ?? string.Empty;
    }

    private static int GetInt32(IReadOnlyDictionary<string, object?> row, string name)
    {
        return Convert.ToInt32(row[name] ?? 0);
    }

    private static int? GetNullableInt32(IReadOnlyDictionary<string, object?> row, string name)
    {
        return row[name] is null ? null : Convert.ToInt32(row[name]);
    }

    private static decimal GetDecimal(IReadOnlyDictionary<string, object?> row, string name)
    {
        return Convert.ToDecimal(row[name] ?? 0);
    }

    private static decimal? GetNullableDecimal(IReadOnlyDictionary<string, object?> row, string name)
    {
        return row[name] is null ? null : Convert.ToDecimal(row[name]);
    }

    private static bool GetBoolean(IReadOnlyDictionary<string, object?> row, string name)
    {
        return Convert.ToBoolean(row[name] ?? false);
    }

    private static DateTime GetDateTime(IReadOnlyDictionary<string, object?> row, string name)
    {
        return Convert.ToDateTime(row[name]);
    }

    private static DateTime? GetNullableDateTime(IReadOnlyDictionary<string, object?> row, string name)
    {
        return row[name] is null ? null : Convert.ToDateTime(row[name]);
    }
}
