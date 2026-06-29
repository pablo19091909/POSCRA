/*
    Rollback 009 - Idempotencia Caja API.
    NO EJECUTAR sin aprobacion explicita posterior.
    No toca movimiento_caja, caja_turno, ventas ni tablas historicas.
*/

SET XACT_ABORT ON;
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.caja_idempotencia', N'U') IS NULL
BEGIN
    PRINT N'dbo.caja_idempotencia no existe. No hay rollback que aplicar.';
    RETURN;
END;

IF EXISTS (SELECT 1 FROM dbo.caja_idempotencia)
BEGIN
    THROW 51009, 'Rollback bloqueado: dbo.caja_idempotencia contiene registros. Requiere validacion manual.', 1;
END;

BEGIN TRANSACTION;

DROP TABLE dbo.caja_idempotencia;

COMMIT TRANSACTION;
