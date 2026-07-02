/*
    Fase 5B.3 - Soporte persistente aditivo para reversa inmutable de VentaEfectivo.

    - Ejecutar solo en Environment=Test con writes_allowed_for_testing=1.
    - No hace backfill.
    - No modifica ventas, pagos, detalles, inventario ni movimientos existentes.
    - No borra ni actualiza datos de negocio.
*/

SET XACT_ABORT ON;
SET NOCOUNT ON;

IF NOT EXISTS (
    SELECT 1
    FROM dbo.app_environment
    WHERE id = 1
      AND environment_name = N'Test'
      AND writes_allowed_for_testing = 1
)
BEGIN
    THROW 51110, 'La migracion 011 solo puede ejecutarse en Environment=Test.', 1;
END;

IF OBJECT_ID(N'dbo.ventas', N'U') IS NULL
    THROW 51111, 'No existe dbo.ventas.', 1;

IF OBJECT_ID(N'dbo.venta_pago', N'U') IS NULL
    THROW 51112, 'No existe dbo.venta_pago.', 1;

IF OBJECT_ID(N'dbo.venta_idempotencia', N'U') IS NULL
    THROW 51113, 'No existe dbo.venta_idempotencia.', 1;

IF OBJECT_ID(N'dbo.venta_auditoria', N'U') IS NULL
    THROW 51114, 'No existe dbo.venta_auditoria.', 1;

IF OBJECT_ID(N'dbo.movimiento_caja', N'U') IS NULL
    THROW 51115, 'No existe dbo.movimiento_caja.', 1;

IF OBJECT_ID(N'dbo.usuario', N'U') IS NULL
    THROW 51116, 'No existe dbo.usuario.', 1;

BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.venta_reversa', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.venta_reversa
    (
        idReversa BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_venta_reversa PRIMARY KEY,
        factura INT NOT NULL,
        idPago BIGINT NOT NULL,
        idMovimientoVentaEfectivo BIGINT NOT NULL,
        idMovimientoCompensatorio BIGINT NULL,
        idIdempotencia BIGINT NOT NULL,
        usuario_id INT NOT NULL,
        motivo NVARCHAR(250) NOT NULL,
        monto DECIMAL(18,2) NOT NULL,
        moneda CHAR(3) NOT NULL
            CONSTRAINT DF_venta_reversa_moneda DEFAULT ('CRC'),
        estado NVARCHAR(20) NOT NULL
            CONSTRAINT DF_venta_reversa_estado DEFAULT (N'Confirmada'),
        fecha_hora_utc DATETIME2(0) NOT NULL
            CONSTRAINT DF_venta_reversa_fecha_hora_utc DEFAULT (SYSUTCDATETIME()),
        trace_id NVARCHAR(100) NULL,
        observaciones NVARCHAR(250) NULL,
        creado_utc DATETIME2(0) NOT NULL
            CONSTRAINT DF_venta_reversa_creado_utc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_venta_reversa_ventas
            FOREIGN KEY (factura) REFERENCES dbo.ventas(factura),
        CONSTRAINT FK_venta_reversa_pago
            FOREIGN KEY (idPago) REFERENCES dbo.venta_pago(idPago),
        CONSTRAINT FK_venta_reversa_movimiento_original
            FOREIGN KEY (idMovimientoVentaEfectivo) REFERENCES dbo.movimiento_caja(idMovimiento),
        CONSTRAINT FK_venta_reversa_movimiento_compensatorio
            FOREIGN KEY (idMovimientoCompensatorio) REFERENCES dbo.movimiento_caja(idMovimiento),
        CONSTRAINT FK_venta_reversa_idempotencia
            FOREIGN KEY (idIdempotencia) REFERENCES dbo.venta_idempotencia(idIdempotencia),
        CONSTRAINT FK_venta_reversa_usuario
            FOREIGN KEY (usuario_id) REFERENCES dbo.usuario(idUsuario),
        CONSTRAINT CK_venta_reversa_monto
            CHECK (monto > 0),
        CONSTRAINT CK_venta_reversa_moneda
            CHECK (moneda = 'CRC'),
        CONSTRAINT CK_venta_reversa_estado
            CHECK (estado IN (N'Confirmada')),
        CONSTRAINT CK_venta_reversa_motivo
            CHECK (LEN(LTRIM(RTRIM(motivo))) > 0)
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.venta_reversa')
      AND name = N'UX_venta_reversa_factura_activa'
)
BEGIN
    CREATE UNIQUE INDEX UX_venta_reversa_factura_activa
        ON dbo.venta_reversa(factura)
        WHERE estado = N'Confirmada';
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.venta_reversa')
      AND name = N'UX_venta_reversa_pago_activa'
)
BEGIN
    CREATE UNIQUE INDEX UX_venta_reversa_pago_activa
        ON dbo.venta_reversa(idPago)
        WHERE estado = N'Confirmada';
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.venta_reversa')
      AND name = N'UX_venta_reversa_movimiento_compensatorio'
)
BEGIN
    CREATE UNIQUE INDEX UX_venta_reversa_movimiento_compensatorio
        ON dbo.venta_reversa(idMovimientoCompensatorio)
        WHERE idMovimientoCompensatorio IS NOT NULL;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.venta_reversa')
      AND name = N'UX_venta_reversa_idempotencia'
)
BEGIN
    CREATE UNIQUE INDEX UX_venta_reversa_idempotencia
        ON dbo.venta_reversa(idIdempotencia);
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.venta_reversa')
      AND name = N'IX_venta_reversa_fecha'
)
BEGIN
    CREATE INDEX IX_venta_reversa_fecha
        ON dbo.venta_reversa(fecha_hora_utc)
        INCLUDE (factura, idPago, monto, usuario_id);
END;

IF EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.venta_auditoria')
      AND name = N'CK_venta_auditoria_evento'
)
BEGIN
    ALTER TABLE dbo.venta_auditoria DROP CONSTRAINT CK_venta_auditoria_evento;
END;

ALTER TABLE dbo.venta_auditoria WITH CHECK
ADD CONSTRAINT CK_venta_auditoria_evento
CHECK (evento IN (
    N'VentaCreada',
    N'VentaAnulada',
    N'VentaDevuelta',
    N'VentaReversada',
    N'PagoRegistrado',
    N'ErrorDeProcesamiento',
    N'AjusteAutorizado'
));

COMMIT TRANSACTION;

SELECT 'MIGRATION_011_REVERSA_VENTA_EFECTIVO_INMUTABLE_OK' AS result;
