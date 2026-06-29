/*
    Fase futura - Migracion 009 Idempotencia Caja API.
    NO EJECUTAR sin aprobacion explicita posterior.
    Script aditivo, sin backfill, sin tocar tablas historicas ni venta_idempotencia.
*/

SET XACT_ABORT ON;
SET NOCOUNT ON;

BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.caja_idempotencia', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.caja_idempotencia
    (
        idCajaIdempotencia bigint IDENTITY(1,1) NOT NULL,
        usuario_id int NOT NULL,
        idTurno bigint NULL,
        caja_codigo nvarchar(50) NULL,
        operacion nvarchar(40) NOT NULL,
        idempotency_key uniqueidentifier NOT NULL,
        request_hash varbinary(32) NOT NULL,
        estado nvarchar(20) NOT NULL,
        idMovimiento bigint NULL,
        cierre_referencia_id bigint NULL,
        resultado_codigo nvarchar(80) NULL,
        creado_utc datetime2(3) NOT NULL CONSTRAINT DF_caja_idempotencia_creado_utc DEFAULT SYSUTCDATETIME(),
        actualizado_utc datetime2(3) NOT NULL CONSTRAINT DF_caja_idempotencia_actualizado_utc DEFAULT SYSUTCDATETIME(),
        completado_utc datetime2(3) NULL,
        metadata_minima nvarchar(250) NULL,
        row_version rowversion NOT NULL,
        CONSTRAINT PK_caja_idempotencia PRIMARY KEY CLUSTERED (idCajaIdempotencia),
        CONSTRAINT FK_caja_idempotencia_usuario FOREIGN KEY (usuario_id) REFERENCES dbo.usuario(idUsuario),
        CONSTRAINT FK_caja_idempotencia_turno FOREIGN KEY (idTurno) REFERENCES dbo.caja_turno(idTurno),
        CONSTRAINT FK_caja_idempotencia_movimiento FOREIGN KEY (idMovimiento) REFERENCES dbo.movimiento_caja(idMovimiento),
        CONSTRAINT CK_caja_idempotencia_operacion CHECK (operacion IN
            (N'IngresoCaja', N'RetiroCaja', N'CerrarTurno', N'AjusteCaja', N'ReversaMovimiento')),
        CONSTRAINT CK_caja_idempotencia_estado CHECK (estado IN
            (N'EnProceso', N'Completada', N'Fallida')),
        CONSTRAINT CK_caja_idempotencia_fechas CHECK
            (completado_utc IS NULL OR completado_utc >= creado_utc),
        CONSTRAINT CK_caja_idempotencia_completada_referencia CHECK
            (
                estado <> N'Completada'
                OR idMovimiento IS NOT NULL
                OR cierre_referencia_id IS NOT NULL
            ),
        CONSTRAINT CK_caja_idempotencia_hash_len CHECK (DATALENGTH(request_hash) = 32)
    );

    CREATE UNIQUE INDEX UX_caja_idempotencia_usuario_operacion_key
        ON dbo.caja_idempotencia (usuario_id, operacion, idempotency_key);

    CREATE INDEX IX_caja_idempotencia_estado_actualizado
        ON dbo.caja_idempotencia (estado, actualizado_utc);

    CREATE INDEX IX_caja_idempotencia_turno_operacion
        ON dbo.caja_idempotencia (idTurno, operacion, estado);

    CREATE UNIQUE INDEX UX_caja_idempotencia_movimiento
        ON dbo.caja_idempotencia (idMovimiento)
        WHERE idMovimiento IS NOT NULL;

    CREATE INDEX IX_caja_idempotencia_caja_operacion
        ON dbo.caja_idempotencia (caja_codigo, operacion, estado);
END;

COMMIT TRANSACTION;
