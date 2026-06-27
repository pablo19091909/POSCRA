/*
    Seed sintetico para pruebas transaccionales de POST /api/ventas.

    USO EXCLUSIVO EN BASES TEST AUTORIZADAS.

    No crea ventas, detalles, pagos, auditorias, idempotencias, ingresos,
    retiros ni cierres.
*/

SET XACT_ABORT ON;

IF NOT EXISTS (
    SELECT 1
    FROM dbo.app_environment
    WHERE id = 1
      AND environment_name = N'Test'
      AND writes_allowed_for_testing = 1
)
BEGIN
    THROW 53100, 'Seed detenido: la base no esta marcada como Test.', 1;
END;

BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM dbo.usuario WHERE nombre = N'API_TEST_ADMIN_VENTAS')
BEGIN
    INSERT INTO dbo.usuario (nombre, contrasena, rol, activo)
    VALUES (N'API_TEST_ADMIN_VENTAS', REPLICATE(N'0', 64), N'Administrador', 1);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.usuario WHERE nombre = N'API_TEST_SIN_PERMISO')
BEGIN
    INSERT INTO dbo.usuario (nombre, contrasena, rol, activo)
    VALUES (N'API_TEST_SIN_PERMISO', REPLICATE(N'0', 64), N'SinPermiso', 1);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.cliente WHERE nombre = N'API_TEST_CLIENTE_EFECTIVO')
BEGIN
    INSERT INTO dbo.cliente (nombre, saldo, comprobante, fecha_carga_saldo)
    VALUES (N'API_TEST_CLIENTE_EFECTIVO', 0.00, N'API_TEST', SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.cliente WHERE nombre = N'API_TEST_CLIENTE_TARJETA')
BEGIN
    INSERT INTO dbo.cliente (nombre, saldo, comprobante, fecha_carga_saldo)
    VALUES (N'API_TEST_CLIENTE_TARJETA', 0.00, N'API_TEST', SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.cliente WHERE nombre = N'API_TEST_CLIENTE_SINPE')
BEGIN
    INSERT INTO dbo.cliente (nombre, saldo, comprobante, fecha_carga_saldo)
    VALUES (N'API_TEST_CLIENTE_SINPE', 0.00, N'API_TEST', SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.cliente WHERE nombre = N'API_TEST_CLIENTE_SALDO_OK')
BEGIN
    INSERT INTO dbo.cliente (nombre, saldo, comprobante, fecha_carga_saldo)
    VALUES (N'API_TEST_CLIENTE_SALDO_OK', 1000.00, N'API_TEST', SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.cliente WHERE nombre = N'API_TEST_CLIENTE_SALDO_BAJO')
BEGIN
    INSERT INTO dbo.cliente (nombre, saldo, comprobante, fecha_carga_saldo)
    VALUES (N'API_TEST_CLIENTE_SALDO_BAJO', 1.00, N'API_TEST', SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.inventario WHERE idProducto = N'API_TEST_PROD_STOCK_ALTO')
BEGIN
    INSERT INTO dbo.inventario (idProducto, nombre, proveedor, costo, precio, stock, vendido)
    VALUES (N'API_TEST_PROD_STOCK_ALTO', N'API_TEST_PROD_STOCK_ALTO', N'API_TEST', 1.00, 10.00, 100, 0);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.inventario WHERE idProducto = N'API_TEST_PROD_STOCK_UNIDAD')
BEGIN
    INSERT INTO dbo.inventario (idProducto, nombre, proveedor, costo, precio, stock, vendido)
    VALUES (N'API_TEST_PROD_STOCK_UNIDAD', N'API_TEST_PROD_STOCK_UNIDAD', N'API_TEST', 1.00, 15.00, 1, 0);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.inventario WHERE idProducto = N'API_TEST_PROD_STOCK_CERO')
BEGIN
    INSERT INTO dbo.inventario (idProducto, nombre, proveedor, costo, precio, stock, vendido)
    VALUES (N'API_TEST_PROD_STOCK_CERO', N'API_TEST_PROD_STOCK_CERO', N'API_TEST', 1.00, 20.00, 0, 0);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.inventario WHERE idProducto = N'API_TEST_PROD_PRECIO_DECIMAL')
BEGIN
    INSERT INTO dbo.inventario (idProducto, nombre, proveedor, costo, precio, stock, vendido)
    VALUES (N'API_TEST_PROD_PRECIO_DECIMAL', N'API_TEST_PROD_PRECIO_DECIMAL', N'API_TEST', 1.00, 12.50, 100, 0);
END;

COMMIT TRANSACTION;
