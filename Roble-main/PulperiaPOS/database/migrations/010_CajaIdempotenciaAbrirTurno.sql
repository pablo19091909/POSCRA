SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF NOT EXISTS (
    SELECT 1
    FROM dbo.app_environment
    WHERE id = 1
      AND environment_name = N'Test'
      AND writes_allowed_for_testing = 1
)
BEGIN
    THROW 51010, 'La migracion 010 solo puede ejecutarse en Environment=Test.', 1;
END;

IF OBJECT_ID(N'dbo.caja_idempotencia', N'U') IS NULL
BEGIN
    THROW 51011, 'No existe dbo.caja_idempotencia.', 1;
END;

IF EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = N'CK_caja_idempotencia_operacion'
      AND parent_object_id = OBJECT_ID(N'dbo.caja_idempotencia')
)
BEGIN
    ALTER TABLE dbo.caja_idempotencia
    DROP CONSTRAINT CK_caja_idempotencia_operacion;
END;

ALTER TABLE dbo.caja_idempotencia WITH CHECK
ADD CONSTRAINT CK_caja_idempotencia_operacion
CHECK (operacion IN (
    N'AbrirTurno',
    N'IngresoCaja',
    N'RetiroCaja',
    N'CerrarTurno',
    N'AjusteCaja',
    N'ReversaMovimiento'
));

COMMIT TRANSACTION;

SELECT 'MIGRATION_010_CAJA_IDEMPOTENCIA_ABRIR_TURNO_OK' AS result;
