/*
    Rollback Fase 3B.1 - Modernizacion aditiva de autenticacion.

    ADVERTENCIA CRITICA:
    No ejecutar si POS.Api ya autentica usuarios con password_hash_v2.
    Este rollback elimina columnas agregadas por la migracion y podria borrar hashes modernos.

    Ejecutar primero database/diagnostics/006_DiagnosticoUsuariosAuth.sql y confirmar
    que no existan usuarios con password_hash_v2 poblado.
*/

SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.usuario', N'U') IS NULL
BEGIN
    THROW 51000, 'No existe la tabla dbo.usuario.', 1;
END;

IF COL_LENGTH(N'dbo.usuario', N'password_hash_v2') IS NOT NULL
   AND EXISTS (SELECT 1 FROM dbo.usuario WHERE password_hash_v2 IS NOT NULL)
BEGIN
    THROW 51001, 'Rollback detenido: existen hashes modernos en password_hash_v2.', 1;
END;

BEGIN TRANSACTION;

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.usuario')
      AND name = N'IX_usuario_nombre_auth'
)
BEGIN
    DROP INDEX IX_usuario_nombre_auth ON dbo.usuario;
END;

IF COL_LENGTH(N'dbo.usuario', N'actualizado_utc') IS NOT NULL
    ALTER TABLE dbo.usuario DROP COLUMN actualizado_utc;

IF COL_LENGTH(N'dbo.usuario', N'creado_utc') IS NOT NULL
    ALTER TABLE dbo.usuario DROP COLUMN creado_utc;

IF COL_LENGTH(N'dbo.usuario', N'password_migrada_utc') IS NOT NULL
    ALTER TABLE dbo.usuario DROP COLUMN password_migrada_utc;

IF COL_LENGTH(N'dbo.usuario', N'ultimo_login_utc') IS NOT NULL
    ALTER TABLE dbo.usuario DROP COLUMN ultimo_login_utc;

IF COL_LENGTH(N'dbo.usuario', N'bloqueado_hasta_utc') IS NOT NULL
    ALTER TABLE dbo.usuario DROP COLUMN bloqueado_hasta_utc;

IF COL_LENGTH(N'dbo.usuario', N'intentos_fallidos') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_usuario_intentos_fallidos')
        ALTER TABLE dbo.usuario DROP CONSTRAINT DF_usuario_intentos_fallidos;

    ALTER TABLE dbo.usuario DROP COLUMN intentos_fallidos;
END;

IF COL_LENGTH(N'dbo.usuario', N'activo') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_usuario_activo')
        ALTER TABLE dbo.usuario DROP CONSTRAINT DF_usuario_activo;

    ALTER TABLE dbo.usuario DROP COLUMN activo;
END;

IF COL_LENGTH(N'dbo.usuario', N'password_hash_version') IS NOT NULL
    ALTER TABLE dbo.usuario DROP COLUMN password_hash_version;

IF COL_LENGTH(N'dbo.usuario', N'password_hash_v2') IS NOT NULL
    ALTER TABLE dbo.usuario DROP COLUMN password_hash_v2;

COMMIT TRANSACTION;
