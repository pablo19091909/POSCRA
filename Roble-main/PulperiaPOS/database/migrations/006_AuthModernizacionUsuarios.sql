/*
    Fase 3B.1 - Modernizacion aditiva de autenticacion para usuario.

    IMPORTANTE:
    - No ejecutar sin respaldo previo.
    - No elimina ni modifica la columna usuario.contrasena.
    - No migra hashes por si solo.
    - No marca usuarios actuales como inactivos.
    - Script idempotente para SQL Server.
*/

SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.usuario', N'U') IS NULL
BEGIN
    THROW 51000, 'No existe la tabla dbo.usuario.', 1;
END;

IF COL_LENGTH(N'dbo.usuario', N'password_hash_v2') IS NULL
BEGIN
    ALTER TABLE dbo.usuario
        ADD password_hash_v2 NVARCHAR(255) NULL;
END;

IF COL_LENGTH(N'dbo.usuario', N'password_hash_version') IS NULL
BEGIN
    ALTER TABLE dbo.usuario
        ADD password_hash_version NVARCHAR(50) NULL;
END;

IF COL_LENGTH(N'dbo.usuario', N'activo') IS NULL
BEGIN
    ALTER TABLE dbo.usuario
        ADD activo BIT NOT NULL
            CONSTRAINT DF_usuario_activo DEFAULT (1);
END;

IF COL_LENGTH(N'dbo.usuario', N'intentos_fallidos') IS NULL
BEGIN
    ALTER TABLE dbo.usuario
        ADD intentos_fallidos INT NOT NULL
            CONSTRAINT DF_usuario_intentos_fallidos DEFAULT (0);
END;

IF COL_LENGTH(N'dbo.usuario', N'bloqueado_hasta_utc') IS NULL
BEGIN
    ALTER TABLE dbo.usuario
        ADD bloqueado_hasta_utc DATETIME2(0) NULL;
END;

IF COL_LENGTH(N'dbo.usuario', N'ultimo_login_utc') IS NULL
BEGIN
    ALTER TABLE dbo.usuario
        ADD ultimo_login_utc DATETIME2(0) NULL;
END;

IF COL_LENGTH(N'dbo.usuario', N'password_migrada_utc') IS NULL
BEGIN
    ALTER TABLE dbo.usuario
        ADD password_migrada_utc DATETIME2(0) NULL;
END;

IF COL_LENGTH(N'dbo.usuario', N'creado_utc') IS NULL
BEGIN
    ALTER TABLE dbo.usuario
        ADD creado_utc DATETIME2(0) NULL;
END;

IF COL_LENGTH(N'dbo.usuario', N'actualizado_utc') IS NULL
BEGIN
    ALTER TABLE dbo.usuario
        ADD actualizado_utc DATETIME2(0) NULL;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.usuario')
      AND name = N'IX_usuario_nombre_auth'
)
BEGIN
    CREATE INDEX IX_usuario_nombre_auth
        ON dbo.usuario(nombre)
        INCLUDE (rol, activo, bloqueado_hasta_utc);
END;

COMMIT TRANSACTION;
