/*
    Fase 4F.8 - Diagnostico de idempotencia Caja API.
    SOLO LECTURA. No ejecutar cambios desde este archivo.
*/

SET NOCOUNT ON;

SELECT
    'environment_test_ok' AS check_name,
    CASE WHEN EXISTS (
        SELECT 1
        FROM dbo.app_environment
        WHERE id = 1
          AND environment_name = N'Test'
          AND writes_allowed_for_testing = 1
    ) THEN 1 ELSE 0 END AS check_value;

SELECT
    'caja_idempotencia_exists' AS check_name,
    CASE WHEN OBJECT_ID(N'dbo.caja_idempotencia', N'U') IS NULL THEN 0 ELSE 1 END AS check_value;

SELECT
    s.name AS schema_name,
    o.name AS object_name,
    o.type_desc
FROM sys.objects o
JOIN sys.schemas s ON s.schema_id = o.schema_id
WHERE s.name = N'dbo'
  AND o.name LIKE N'%caja%idempot%';

SELECT
    table_name = t.name,
    c.column_id,
    column_name = c.name,
    type_name = ty.name,
    c.max_length,
    c.precision,
    c.scale,
    c.is_nullable
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.columns c ON c.object_id = t.object_id
JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE s.name = N'dbo'
  AND t.name IN (N'caja_turno', N'movimiento_caja', N'venta_idempotencia')
ORDER BY t.name, c.column_id;

SELECT
    table_name = t.name,
    index_name = i.name,
    i.is_unique,
    i.has_filter,
    i.filter_definition
FROM sys.indexes i
JOIN sys.tables t ON t.object_id = i.object_id
JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name = N'dbo'
  AND t.name IN (N'caja_turno', N'movimiento_caja', N'venta_idempotencia')
  AND i.name IS NOT NULL
ORDER BY t.name, i.name;

SELECT
    table_name = OBJECT_NAME(cc.parent_object_id),
    check_name = cc.name,
    cc.definition
FROM sys.check_constraints cc
WHERE OBJECT_SCHEMA_NAME(cc.parent_object_id) = N'dbo'
  AND OBJECT_NAME(cc.parent_object_id) IN (N'caja_turno', N'movimiento_caja', N'venta_idempotencia')
ORDER BY table_name, check_name;

SELECT
    foreign_key_name = fk.name,
    parent_table = OBJECT_NAME(fk.parent_object_id),
    referenced_table = OBJECT_NAME(fk.referenced_object_id)
FROM sys.foreign_keys fk
WHERE OBJECT_SCHEMA_NAME(fk.parent_object_id) = N'dbo'
  AND (
      OBJECT_NAME(fk.parent_object_id) IN (N'caja_turno', N'movimiento_caja', N'venta_idempotencia')
      OR OBJECT_NAME(fk.referenced_object_id) IN (N'caja_turno', N'movimiento_caja', N'venta_idempotencia')
  )
ORDER BY parent_table, foreign_key_name;

SELECT
    proposed_name,
    conflict_type,
    existing_object_name
FROM
(
    VALUES
        (N'PK_caja_idempotencia'),
        (N'FK_caja_idempotencia_usuario'),
        (N'FK_caja_idempotencia_turno'),
        (N'FK_caja_idempotencia_movimiento'),
        (N'CK_caja_idempotencia_operacion'),
        (N'CK_caja_idempotencia_estado'),
        (N'CK_caja_idempotencia_fechas'),
        (N'CK_caja_idempotencia_completada_referencia'),
        (N'CK_caja_idempotencia_hash_len'),
        (N'UX_caja_idempotencia_usuario_operacion_key'),
        (N'IX_caja_idempotencia_estado_actualizado'),
        (N'IX_caja_idempotencia_turno_operacion'),
        (N'UX_caja_idempotencia_movimiento'),
        (N'IX_caja_idempotencia_caja_operacion')
) proposed(proposed_name)
OUTER APPLY
(
    SELECT TOP (1)
        conflict_type = o.type_desc,
        existing_object_name = o.name
    FROM sys.objects o
    WHERE o.name = proposed.proposed_name
    UNION ALL
    SELECT TOP (1)
        conflict_type = N'INDEX',
        existing_object_name = i.name
    FROM sys.indexes i
    WHERE i.name = proposed.proposed_name
) existing
WHERE existing.existing_object_name IS NOT NULL
ORDER BY proposed_name;

SELECT 'caja_turno_rows' AS metric, COUNT_BIG(1) AS value FROM dbo.caja_turno
UNION ALL SELECT 'movimiento_caja_rows', COUNT_BIG(1) FROM dbo.movimiento_caja
UNION ALL SELECT 'ingreso_caja_rows', COUNT_BIG(1) FROM dbo.ingreso_caja
UNION ALL SELECT 'retiro_caja_rows', COUNT_BIG(1) FROM dbo.retiro_caja
UNION ALL SELECT 'cierre_caja_rows', COUNT_BIG(1) FROM dbo.cierre_caja
UNION ALL SELECT 'ingresos_api', COUNT_BIG(1) FROM dbo.movimiento_caja WHERE tipo_movimiento = N'IngresoCaja'
UNION ALL SELECT 'retiros_api', COUNT_BIG(1) FROM dbo.movimiento_caja WHERE tipo_movimiento = N'RetiroCaja';
