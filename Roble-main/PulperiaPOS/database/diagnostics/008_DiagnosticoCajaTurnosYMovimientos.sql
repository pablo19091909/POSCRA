/*
    Fase 4F.1 - Diagnostico previo para CajaTurno y MovimientoCaja.

    IMPORTANTE:
    - SOLO SELECT y metadatos.
    - NO modifica datos.
    - NO crea objetos.
    - NO ejecutar contra produccion sin aprobacion formal.
*/

SET NOCOUNT ON;

SELECT
    DB_NAME() AS database_name,
    SYSUTCDATETIME() AS diagnostic_utc;

SELECT
    name AS table_name,
    object_id,
    create_date,
    modify_date
FROM sys.tables
WHERE name IN (
    N'ventas',
    N'DetalleVenta',
    N'venta_pago',
    N'venta_auditoria',
    N'venta_idempotencia',
    N'ingreso_caja',
    N'retiro_caja',
    N'cierre_caja',
    N'usuario',
    N'cliente',
    N'caja_turno',
    N'movimiento_caja'
)
ORDER BY name;

SELECT
    t.name AS table_name,
    c.column_id,
    c.name AS column_name,
    ty.name AS data_type,
    c.max_length,
    c.precision,
    c.scale,
    c.is_nullable
FROM sys.tables t
JOIN sys.columns c ON c.object_id = t.object_id
JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE t.name IN (
    N'ventas',
    N'venta_pago',
    N'ingreso_caja',
    N'retiro_caja',
    N'cierre_caja',
    N'usuario',
    N'caja_turno',
    N'movimiento_caja'
)
ORDER BY t.name, c.column_id;

SELECT 'ventas' AS table_name, COUNT_BIG(*) AS row_count FROM dbo.ventas
UNION ALL SELECT 'venta_pago', COUNT_BIG(*) FROM dbo.venta_pago
UNION ALL SELECT 'venta_auditoria', COUNT_BIG(*) FROM dbo.venta_auditoria
UNION ALL SELECT 'venta_idempotencia', COUNT_BIG(*) FROM dbo.venta_idempotencia
UNION ALL SELECT 'ingreso_caja', COUNT_BIG(*) FROM dbo.ingreso_caja
UNION ALL SELECT 'retiro_caja', COUNT_BIG(*) FROM dbo.retiro_caja
UNION ALL SELECT 'cierre_caja', COUNT_BIG(*) FROM dbo.cierre_caja;

SELECT
    metodo_pago,
    COUNT_BIG(*) AS cantidad,
    SUM(COALESCE(total, 0)) AS total_agregado
FROM dbo.ventas
GROUP BY metodo_pago
ORDER BY metodo_pago;

SELECT
    vp.metodo_pago,
    vp.moneda,
    vp.estado,
    COUNT_BIG(*) AS cantidad,
    SUM(vp.monto) AS total_agregado
FROM dbo.venta_pago vp
GROUP BY vp.metodo_pago, vp.moneda, vp.estado
ORDER BY vp.metodo_pago, vp.moneda, vp.estado;

SELECT
    'ingresos_sin_usuario_fk' AS diagnostic,
    COUNT_BIG(*) AS cantidad
FROM dbo.ingreso_caja
WHERE usuario IS NULL OR LTRIM(RTRIM(usuario)) = N''
UNION ALL
SELECT
    'retiros_sin_usuario_en_tabla',
    COUNT_BIG(*)
FROM dbo.retiro_caja
UNION ALL
SELECT
    'cierres_sin_turno',
    COUNT_BIG(*)
FROM dbo.cierre_caja;

IF OBJECT_ID(N'dbo.caja_turno', N'U') IS NOT NULL
    SELECT estado, COUNT_BIG(*) AS cantidad FROM dbo.caja_turno GROUP BY estado;

IF OBJECT_ID(N'dbo.movimiento_caja', N'U') IS NOT NULL
    SELECT tipo_movimiento, estado, COUNT_BIG(*) AS cantidad FROM dbo.movimiento_caja GROUP BY tipo_movimiento, estado;

SELECT
    i.name AS index_name,
    OBJECT_NAME(i.object_id) AS table_name,
    i.has_filter,
    i.filter_definition
FROM sys.indexes i
WHERE i.name IN (
    N'IX_movimiento_caja_factura',
    N'IX_movimiento_caja_pago',
    N'IX_movimiento_caja_factura_pago'
)
ORDER BY table_name, index_name;
