/*
    Diagnostico Fase 4B.1 - Soporte para ventas transaccionales.

    SOLO LECTURA.
    No muestra ventas, clientes, productos, usuarios, comprobantes ni montos individuales.
    No ejecutar como migracion.
*/

SET NOCOUNT ON;

SELECT 'tabla_existe_ventas' AS metrica, CASE WHEN OBJECT_ID(N'dbo.ventas', N'U') IS NULL THEN 0 ELSE 1 END AS valor
UNION ALL SELECT 'tabla_existe_DetalleVenta', CASE WHEN OBJECT_ID(N'dbo.DetalleVenta', N'U') IS NULL THEN 0 ELSE 1 END
UNION ALL SELECT 'tabla_existe_inventario', CASE WHEN OBJECT_ID(N'dbo.inventario', N'U') IS NULL THEN 0 ELSE 1 END
UNION ALL SELECT 'tabla_existe_cliente', CASE WHEN OBJECT_ID(N'dbo.cliente', N'U') IS NULL THEN 0 ELSE 1 END
UNION ALL SELECT 'tabla_existe_usuario', CASE WHEN OBJECT_ID(N'dbo.usuario', N'U') IS NULL THEN 0 ELSE 1 END
UNION ALL SELECT 'tabla_existe_ingreso_caja', CASE WHEN OBJECT_ID(N'dbo.ingreso_caja', N'U') IS NULL THEN 0 ELSE 1 END
UNION ALL SELECT 'tabla_existe_retiro_caja', CASE WHEN OBJECT_ID(N'dbo.retiro_caja', N'U') IS NULL THEN 0 ELSE 1 END
UNION ALL SELECT 'tabla_existe_cierre_caja', CASE WHEN OBJECT_ID(N'dbo.cierre_caja', N'U') IS NULL THEN 0 ELSE 1 END
UNION ALL SELECT 'tabla_existe_TipoCambioDolar', CASE WHEN OBJECT_ID(N'dbo.TipoCambioDolar', N'U') IS NULL THEN 0 ELSE 1 END;

SELECT
    c.TABLE_NAME AS tabla,
    c.COLUMN_NAME AS columna,
    c.DATA_TYPE AS tipo,
    c.IS_NULLABLE AS nullable,
    c.ORDINAL_POSITION AS ordinal
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_NAME IN (
    N'ventas',
    N'DetalleVenta',
    N'inventario',
    N'cliente',
    N'usuario',
    N'ingreso_caja',
    N'retiro_caja',
    N'cierre_caja',
    N'TipoCambioDolar'
)
ORDER BY c.TABLE_NAME, c.ORDINAL_POSITION;

SELECT
    'ventas_columna_pago_vuelto_usuario' AS grupo,
    columna,
    CASE WHEN COL_LENGTH(N'dbo.ventas', columna) IS NULL THEN 'FALTA' ELSE 'EXISTE' END AS estado
FROM (VALUES
    (N'metodo_pago'),
    (N'numero_voucher'),
    (N'numero_comprobante'),
    (N'monto_pagado'),
    (N'vuelto'),
    (N'usuario_id'),
    (N'fecha'),
    (N'hora'),
    (N'total')
) v(columna);

SELECT
    t.name AS tabla,
    c.name AS columna,
    ty.name AS tipo,
    c.is_nullable
FROM sys.columns c
JOIN sys.types ty ON c.user_type_id = ty.user_type_id
JOIN sys.tables t ON c.object_id = t.object_id
WHERE t.name IN (N'ventas', N'DetalleVenta', N'inventario', N'cliente', N'ingreso_caja', N'retiro_caja', N'cierre_caja')
  AND ty.name IN (N'money', N'smallmoney', N'decimal', N'numeric', N'float', N'real')
ORDER BY t.name, c.column_id;

IF OBJECT_ID(N'dbo.ventas', N'U') IS NOT NULL
BEGIN
    SELECT 'ventas_total' AS metrica, COUNT_BIG(*) AS valor FROM dbo.ventas;
    SELECT 'ventas_sin_detalle' AS metrica, COUNT_BIG(*) AS valor
    FROM dbo.ventas v
    WHERE NOT EXISTS (SELECT 1 FROM dbo.DetalleVenta d WHERE d.factura = v.factura);
    SELECT 'ventas_sin_usuario' AS metrica, COUNT_BIG(*) AS valor
    FROM dbo.ventas
    WHERE usuario_id IS NULL;
    SELECT 'ventas_sin_cliente' AS metrica, COUNT_BIG(*) AS valor
    FROM dbo.ventas
    WHERE cliente_id IS NULL;
    SELECT 'ventas_total_nulo_o_invalido' AS metrica, COUNT_BIG(*) AS valor
    FROM dbo.ventas
    WHERE total IS NULL OR total < 0;
    SELECT 'ventas_sin_metodo_pago' AS metrica, COUNT_BIG(*) AS valor
    FROM dbo.ventas
    WHERE metodo_pago IS NULL OR LTRIM(RTRIM(metodo_pago)) = N'';
    SELECT 'ventas_tarjeta_sin_voucher' AS metrica, COUNT_BIG(*) AS valor
    FROM dbo.ventas
    WHERE LOWER(ISNULL(metodo_pago, N'')) IN (N'tarjeta', N'datafono', N'datáfono', N'voucher')
      AND (numero_voucher IS NULL OR LTRIM(RTRIM(numero_voucher)) = N'');
    SELECT 'ventas_sinpe_sin_comprobante' AS metrica, COUNT_BIG(*) AS valor
    FROM dbo.ventas
    WHERE LOWER(ISNULL(metodo_pago, N'')) = N'sinpe'
      AND (numero_comprobante IS NULL OR LTRIM(RTRIM(numero_comprobante)) = N'');
END;

IF OBJECT_ID(N'dbo.DetalleVenta', N'U') IS NOT NULL
BEGIN
    SELECT 'detalles_total' AS metrica, COUNT_BIG(*) AS valor FROM dbo.DetalleVenta;
    SELECT 'detalles_sin_venta' AS metrica, COUNT_BIG(*) AS valor
    FROM dbo.DetalleVenta d
    WHERE NOT EXISTS (SELECT 1 FROM dbo.ventas v WHERE v.factura = d.factura);
    SELECT 'detalles_sin_producto' AS metrica, COUNT_BIG(*) AS valor
    FROM dbo.DetalleVenta d
    WHERE NOT EXISTS (SELECT 1 FROM dbo.inventario i WHERE i.idProducto = d.producto_id);
    SELECT 'detalles_cantidad_invalida' AS metrica, COUNT_BIG(*) AS valor
    FROM dbo.DetalleVenta
    WHERE cantidad <= 0;
    SELECT 'detalles_precio_invalido' AS metrica, COUNT_BIG(*) AS valor
    FROM dbo.DetalleVenta
    WHERE precio_unitario < 0;
END;

IF OBJECT_ID(N'dbo.inventario', N'U') IS NOT NULL
BEGIN
    SELECT 'productos_stock_negativo' AS metrica, COUNT_BIG(*) AS valor
    FROM dbo.inventario
    WHERE ISNULL(stock, 0) < 0;
    SELECT 'productos_precio_nulo_o_invalido' AS metrica, COUNT_BIG(*) AS valor
    FROM dbo.inventario
    WHERE precio IS NULL OR precio < 0;
END;

IF OBJECT_ID(N'dbo.cliente', N'U') IS NOT NULL
BEGIN
    SELECT 'clientes_saldo_negativo' AS metrica, COUNT_BIG(*) AS valor
    FROM dbo.cliente
    WHERE ISNULL(saldo, 0) < 0;
END;

SELECT
    fk.name AS foreign_key,
    OBJECT_NAME(fk.parent_object_id) AS tabla_hija,
    COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS columna_hija,
    OBJECT_NAME(fk.referenced_object_id) AS tabla_padre,
    COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS columna_padre
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
WHERE OBJECT_NAME(fk.parent_object_id) IN (N'ventas', N'DetalleVenta', N'inventario', N'cliente', N'usuario')
   OR OBJECT_NAME(fk.referenced_object_id) IN (N'ventas', N'DetalleVenta', N'inventario', N'cliente', N'usuario')
ORDER BY tabla_hija, foreign_key;

SELECT
    i.name AS indice,
    OBJECT_NAME(i.object_id) AS tabla,
    i.is_unique,
    i.type_desc
FROM sys.indexes i
WHERE OBJECT_NAME(i.object_id) IN (N'ventas', N'DetalleVenta', N'inventario', N'cliente', N'usuario')
ORDER BY tabla, indice;

SELECT
    o.type_desc AS tipo_objeto,
    o.name AS objeto
FROM sys.sql_expression_dependencies d
JOIN sys.objects o ON d.referencing_id = o.object_id
WHERE d.referenced_entity_name IN (N'ventas', N'DetalleVenta')
ORDER BY o.type_desc, o.name;

SELECT
    'tabla_soporte_venta_idempotencia_existe' AS metrica,
    CASE WHEN OBJECT_ID(N'dbo.venta_idempotencia', N'U') IS NULL THEN 0 ELSE 1 END AS valor
UNION ALL SELECT 'tabla_soporte_venta_pago_existe', CASE WHEN OBJECT_ID(N'dbo.venta_pago', N'U') IS NULL THEN 0 ELSE 1 END
UNION ALL SELECT 'tabla_soporte_venta_auditoria_existe', CASE WHEN OBJECT_ID(N'dbo.venta_auditoria', N'U') IS NULL THEN 0 ELSE 1 END;
