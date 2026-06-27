/*
    Marca explicita de ambiente Test para pruebas controladas de escritura API.

    USO EXCLUSIVO EN BASES DE PRUEBA AUTORIZADAS.

    No modifica tablas de negocio.
    No toca ventas, detalle, inventario, clientes, usuarios, caja ni cierres.
*/

SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.app_environment', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.app_environment
    (
        id INT NOT NULL
            CONSTRAINT PK_app_environment PRIMARY KEY,
        environment_name NVARCHAR(40) NOT NULL,
        writes_allowed_for_testing BIT NOT NULL,
        marcado_utc DATETIME2(0) NOT NULL
            CONSTRAINT DF_app_environment_marcado_utc DEFAULT (SYSUTCDATETIME()),
        nota_tecnica NVARCHAR(250) NOT NULL,
        CONSTRAINT CK_app_environment_singleton CHECK (id = 1),
        CONSTRAINT CK_app_environment_name CHECK (environment_name IN (N'Test'))
    );
END;

IF EXISTS (
    SELECT 1
    FROM dbo.app_environment
    WHERE id = 1
      AND (environment_name <> N'Test' OR writes_allowed_for_testing <> 1)
)
BEGIN
    THROW 53001, 'Marca de ambiente existente no compatible con Test.', 1;
END;

IF NOT EXISTS (SELECT 1 FROM dbo.app_environment WHERE id = 1)
BEGIN
    INSERT INTO dbo.app_environment
        (id, environment_name, writes_allowed_for_testing, nota_tecnica)
    VALUES
        (1, N'Test', 1, N'Base autorizada para pruebas transaccionales POS.Api con datos API_TEST_.');
END;

IF NOT EXISTS (
    SELECT 1
    FROM dbo.app_environment
    WHERE id = 1
      AND environment_name = N'Test'
      AND writes_allowed_for_testing = 1
)
BEGIN
    THROW 53002, 'No fue posible confirmar Environment=Test.', 1;
END;

COMMIT TRANSACTION;
