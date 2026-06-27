/*
    Validacion post migracion Fase 4B.1 - Soporte para ventas transaccionales.

    SOLO LECTURA.
    Preparado para ejecutarse despues de aplicar 007_SoporteVentasTransaccionales.sql.
    En Fase 4B.1 no debe ejecutarse contra Azure SQL.
*/

SET NOCOUNT ON;

SELECT 'venta_idempotencia_existe' AS metrica, CASE WHEN OBJECT_ID(N'dbo.venta_idempotencia', N'U') IS NULL THEN 0 ELSE 1 END AS valor
UNION ALL SELECT 'venta_pago_existe', CASE WHEN OBJECT_ID(N'dbo.venta_pago', N'U') IS NULL THEN 0 ELSE 1 END
UNION ALL SELECT 'venta_auditoria_existe', CASE WHEN OBJECT_ID(N'dbo.venta_auditoria', N'U') IS NULL THEN 0 ELSE 1 END;

SELECT
    t.name AS tabla,
    c.name AS columna,
    ty.name AS tipo,
    c.is_nullable,
    c.column_id
FROM sys.tables t
JOIN sys.columns c ON t.object_id = c.object_id
JOIN sys.types ty ON c.user_type_id = ty.user_type_id
WHERE t.name IN (N'venta_idempotencia', N'venta_pago', N'venta_auditoria')
ORDER BY t.name, c.column_id;

SELECT
    OBJECT_NAME(i.object_id) AS tabla,
    i.name AS indice,
    i.is_unique,
    i.type_desc
FROM sys.indexes i
WHERE OBJECT_NAME(i.object_id) IN (N'venta_idempotencia', N'venta_pago', N'venta_auditoria')
ORDER BY tabla, indice;

SELECT
    fk.name AS foreign_key,
    OBJECT_NAME(fk.parent_object_id) AS tabla_hija,
    COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS columna_hija,
    OBJECT_NAME(fk.referenced_object_id) AS tabla_padre,
    COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS columna_padre
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
WHERE OBJECT_NAME(fk.parent_object_id) IN (N'venta_idempotencia', N'venta_pago', N'venta_auditoria')
ORDER BY tabla_hija, foreign_key;

SELECT
    cc.name AS check_constraint,
    OBJECT_NAME(cc.parent_object_id) AS tabla,
    CASE
        WHEN cc.name = N'CK_venta_pago_metodo'
             AND cc.definition LIKE N'%Donación%' THEN 1
        WHEN cc.name = N'CK_venta_pago_montos'
             AND cc.definition LIKE N'%monto]>(0)%'
             AND cc.definition LIKE N'%vuelto] IS NULL%' THEN 1
        WHEN cc.name NOT IN (N'CK_venta_pago_metodo', N'CK_venta_pago_montos') THEN 1
        ELSE 0
    END AS regla_esperada
FROM sys.check_constraints cc
WHERE OBJECT_NAME(cc.parent_object_id) IN (N'venta_idempotencia', N'venta_pago', N'venta_auditoria')
ORDER BY tabla, check_constraint;

SELECT 'venta_pago_vuelto_nullable' AS metrica,
       CASE
           WHEN OBJECT_ID(N'dbo.venta_pago', N'U') IS NULL THEN NULL
           WHEN EXISTS (
               SELECT 1
               FROM sys.columns
               WHERE object_id = OBJECT_ID(N'dbo.venta_pago')
                 AND name = N'vuelto'
                 AND is_nullable = 1
           ) THEN 1
           ELSE 0
       END AS valor
UNION ALL
SELECT 'venta_pago_metodo_incluye_donacion',
       CASE
           WHEN OBJECT_ID(N'dbo.venta_pago', N'U') IS NULL THEN NULL
           WHEN EXISTS (
               SELECT 1
               FROM sys.check_constraints
               WHERE parent_object_id = OBJECT_ID(N'dbo.venta_pago')
                 AND name = N'CK_venta_pago_metodo'
                 AND definition LIKE N'%Donación%'
           ) THEN 1
           ELSE 0
       END
UNION ALL
SELECT 'venta_pago_monto_positivo',
       CASE
           WHEN OBJECT_ID(N'dbo.venta_pago', N'U') IS NULL THEN NULL
           WHEN EXISTS (
               SELECT 1
               FROM sys.check_constraints
               WHERE parent_object_id = OBJECT_ID(N'dbo.venta_pago')
                 AND name = N'CK_venta_pago_montos'
                 AND definition LIKE N'%monto]>(0)%'
           ) THEN 1
           ELSE 0
       END;

SELECT 'venta_idempotencia_registros' AS metrica,
       CASE WHEN OBJECT_ID(N'dbo.venta_idempotencia', N'U') IS NULL THEN NULL ELSE (SELECT COUNT_BIG(*) FROM dbo.venta_idempotencia) END AS valor
UNION ALL
SELECT 'venta_pago_registros',
       CASE WHEN OBJECT_ID(N'dbo.venta_pago', N'U') IS NULL THEN NULL ELSE (SELECT COUNT_BIG(*) FROM dbo.venta_pago) END
UNION ALL
SELECT 'venta_auditoria_registros',
       CASE WHEN OBJECT_ID(N'dbo.venta_auditoria', N'U') IS NULL THEN NULL ELSE (SELECT COUNT_BIG(*) FROM dbo.venta_auditoria) END;

SELECT 'ventas_total_historico' AS metrica, COUNT_BIG(*) AS valor FROM dbo.ventas
UNION ALL SELECT 'detalle_total_historico', COUNT_BIG(*) FROM dbo.DetalleVenta;
