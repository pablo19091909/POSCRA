/*
    Fase 4F.1 - Validacion posterior preparada para migracion 008.

    IMPORTANTE:
    - SOLO SELECT y metadatos.
    - NO modifica datos.
    - Ejecutar solo despues de una aprobacion futura de la migracion 008.
*/

SET NOCOUNT ON;

SELECT
    t.name AS table_name,
    t.create_date,
    t.modify_date
FROM sys.tables t
WHERE t.name IN (N'caja_turno', N'movimiento_caja')
ORDER BY t.name;

SELECT
    i.name AS index_name,
    OBJECT_NAME(i.object_id) AS table_name,
    i.is_unique,
    i.has_filter,
    i.filter_definition
FROM sys.indexes i
WHERE OBJECT_NAME(i.object_id) IN (N'caja_turno', N'movimiento_caja')
ORDER BY table_name, index_name;

SELECT
    i.name AS index_name,
    i.has_filter,
    i.filter_definition
FROM sys.indexes i
WHERE OBJECT_NAME(i.object_id) = N'movimiento_caja'
  AND i.name IN (
      N'IX_movimiento_caja_factura',
      N'IX_movimiento_caja_pago',
      N'IX_movimiento_caja_factura_pago'
  )
ORDER BY i.name;

SELECT
    fk.name AS foreign_key_name,
    OBJECT_NAME(fk.parent_object_id) AS child_table,
    OBJECT_NAME(fk.referenced_object_id) AS parent_table,
    fk.is_disabled,
    fk.is_not_trusted
FROM sys.foreign_keys fk
WHERE OBJECT_NAME(fk.parent_object_id) IN (N'caja_turno', N'movimiento_caja')
ORDER BY child_table, foreign_key_name;

SELECT
    'caja_turno' AS table_name,
    COUNT_BIG(*) AS row_count
FROM dbo.caja_turno
UNION ALL
SELECT
    'movimiento_caja',
    COUNT_BIG(*)
FROM dbo.movimiento_caja;

SELECT
    caja_codigo,
    COUNT_BIG(*) AS turnos_abiertos_o_en_cierre
FROM dbo.caja_turno
WHERE estado IN (N'Abierto', N'EnCierre')
GROUP BY caja_codigo
HAVING COUNT_BIG(*) > 1;

SELECT
    pago_id,
    COUNT_BIG(*) AS movimientos_venta_efectivo
FROM dbo.movimiento_caja
WHERE pago_id IS NOT NULL
  AND tipo_movimiento = N'VentaEfectivo'
  AND estado = N'Confirmado'
GROUP BY pago_id
HAVING COUNT_BIG(*) > 1;

SELECT
    'indices_filtrados_con_or' AS diagnostic,
    COUNT_BIG(*) AS cantidad
FROM sys.indexes i
WHERE OBJECT_NAME(i.object_id) IN (N'caja_turno', N'movimiento_caja')
  AND i.has_filter = 1
  AND UPPER(i.filter_definition) LIKE N'% OR %';
