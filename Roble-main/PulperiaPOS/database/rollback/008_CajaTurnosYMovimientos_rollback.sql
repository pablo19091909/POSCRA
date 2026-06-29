/*
    Fase 4F.1 - Rollback preparado para CajaTurno y MovimientoCaja.

    IMPORTANTE:
    - NO ejecutar sin aprobacion explicita posterior.
    - Se bloquea si existen turnos o movimientos.
    - No toca tablas historicas.
*/

SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.movimiento_caja', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM dbo.movimiento_caja)
BEGIN
    THROW 52100, 'Rollback bloqueado: existen movimientos de caja.', 1;
END;

IF OBJECT_ID(N'dbo.caja_turno', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM dbo.caja_turno)
BEGIN
    THROW 52101, 'Rollback bloqueado: existen turnos de caja.', 1;
END;

BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.movimiento_caja', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.movimiento_caja;
END;

IF OBJECT_ID(N'dbo.caja_turno', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.caja_turno;
END;

COMMIT TRANSACTION;
