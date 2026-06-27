/*
    Rollback Fase 4B.1 - Soporte para ventas transaccionales.

    ADVERTENCIA CRITICA:
    - NO ejecutar si POS.Api ya creo ventas usando estas tablas.
    - NO ejecutar si existe cualquier registro en venta_pago, venta_auditoria o venta_idempotencia.
    - Este rollback elimina tablas de soporte vacias.
    - Ejecutar primero diagnostics/007_ValidacionPostMigracionSoporteVentas.sql.
*/

SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.venta_pago', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM dbo.venta_pago)
BEGIN
    THROW 52001, 'Rollback detenido: dbo.venta_pago contiene registros.', 1;
END;

IF OBJECT_ID(N'dbo.venta_auditoria', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM dbo.venta_auditoria)
BEGIN
    THROW 52002, 'Rollback detenido: dbo.venta_auditoria contiene registros.', 1;
END;

IF OBJECT_ID(N'dbo.venta_idempotencia', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM dbo.venta_idempotencia)
BEGIN
    THROW 52003, 'Rollback detenido: dbo.venta_idempotencia contiene registros.', 1;
END;

BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.venta_auditoria', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.venta_auditoria;
END;

IF OBJECT_ID(N'dbo.venta_pago', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.venta_pago;
END;

IF OBJECT_ID(N'dbo.venta_idempotencia', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.venta_idempotencia;
END;

COMMIT TRANSACTION;
