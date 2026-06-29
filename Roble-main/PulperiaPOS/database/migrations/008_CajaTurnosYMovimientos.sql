/*
    Fase 4F.1 - Diseno preparado para caja operacional.

    IMPORTANTE:
    - SCRIPT PREPARADO, NO EJECUTADO EN FASE 4F.1.
    - No ejecutar sin aprobacion explicita posterior.
    - Migracion aditiva e idempotente.
    - No modifica tablas historicas.
    - No hace backfill.
    - No crea movimientos historicos.
*/

SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.usuario', N'U') IS NULL
    THROW 52000, 'No existe dbo.usuario.', 1;

IF OBJECT_ID(N'dbo.ventas', N'U') IS NULL
    THROW 52001, 'No existe dbo.ventas.', 1;

IF OBJECT_ID(N'dbo.venta_pago', N'U') IS NULL
    THROW 52002, 'No existe dbo.venta_pago.', 1;

IF OBJECT_ID(N'dbo.ingreso_caja', N'U') IS NULL
    THROW 52003, 'No existe dbo.ingreso_caja.', 1;

IF OBJECT_ID(N'dbo.retiro_caja', N'U') IS NULL
    THROW 52004, 'No existe dbo.retiro_caja.', 1;

IF OBJECT_ID(N'dbo.cierre_caja', N'U') IS NULL
    THROW 52005, 'No existe dbo.cierre_caja.', 1;

BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.caja_turno', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.caja_turno
    (
        idTurno BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_caja_turno PRIMARY KEY,
        caja_codigo NVARCHAR(50) NOT NULL,
        estado NVARCHAR(20) NOT NULL
            CONSTRAINT DF_caja_turno_estado DEFAULT (N'Abierto'),
        usuario_apertura_id INT NOT NULL,
        usuario_cierre_id INT NULL,
        apertura_utc DATETIME2(0) NOT NULL
            CONSTRAINT DF_caja_turno_apertura_utc DEFAULT (SYSUTCDATETIME()),
        cierre_utc DATETIME2(0) NULL,
        fondo_inicial DECIMAL(18,2) NOT NULL,
        efectivo_esperado DECIMAL(18,2) NULL,
        efectivo_contado DECIMAL(18,2) NULL,
        diferencia DECIMAL(18,2) NULL,
        observacion_apertura NVARCHAR(250) NULL,
        observacion_cierre NVARCHAR(250) NULL,
        cierre_caja_id INT NULL,
        creado_utc DATETIME2(0) NOT NULL
            CONSTRAINT DF_caja_turno_creado_utc DEFAULT (SYSUTCDATETIME()),
        actualizado_utc DATETIME2(0) NOT NULL
            CONSTRAINT DF_caja_turno_actualizado_utc DEFAULT (SYSUTCDATETIME()),
        row_version ROWVERSION NOT NULL,
        CONSTRAINT FK_caja_turno_usuario_apertura
            FOREIGN KEY (usuario_apertura_id) REFERENCES dbo.usuario(idUsuario),
        CONSTRAINT FK_caja_turno_usuario_cierre
            FOREIGN KEY (usuario_cierre_id) REFERENCES dbo.usuario(idUsuario),
        CONSTRAINT FK_caja_turno_cierre_historico
            FOREIGN KEY (cierre_caja_id) REFERENCES dbo.cierre_caja(idCierre),
        CONSTRAINT CK_caja_turno_estado
            CHECK (estado IN (N'Abierto', N'EnCierre', N'Cerrado', N'Anulado')),
        CONSTRAINT CK_caja_turno_montos
            CHECK (
                fondo_inicial >= 0
                AND (efectivo_esperado IS NULL OR efectivo_esperado >= 0)
                AND (efectivo_contado IS NULL OR efectivo_contado >= 0)
            ),
        CONSTRAINT CK_caja_turno_fechas
            CHECK (cierre_utc IS NULL OR cierre_utc >= apertura_utc),
        CONSTRAINT CK_caja_turno_cierre_requerido
            CHECK (
                estado NOT IN (N'Cerrado', N'Anulado')
                OR (usuario_cierre_id IS NOT NULL AND cierre_utc IS NOT NULL)
            )
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.caja_turno')
      AND name = N'UX_caja_turno_un_abierto_por_caja'
)
BEGIN
    CREATE UNIQUE INDEX UX_caja_turno_un_abierto_por_caja
        ON dbo.caja_turno(caja_codigo)
        WHERE estado IN (N'Abierto', N'EnCierre');
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.caja_turno')
      AND name = N'IX_caja_turno_estado_apertura'
)
BEGIN
    CREATE INDEX IX_caja_turno_estado_apertura
        ON dbo.caja_turno(estado, apertura_utc)
        INCLUDE (caja_codigo, usuario_apertura_id, cierre_utc);
END;

IF OBJECT_ID(N'dbo.movimiento_caja', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.movimiento_caja
    (
        idMovimiento BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_movimiento_caja PRIMARY KEY,
        idTurno BIGINT NOT NULL,
        tipo_movimiento NVARCHAR(30) NOT NULL,
        origen NVARCHAR(30) NOT NULL,
        monto DECIMAL(18,2) NOT NULL,
        moneda CHAR(3) NOT NULL
            CONSTRAINT DF_movimiento_caja_moneda DEFAULT ('CRC'),
        fecha_hora_utc DATETIME2(0) NOT NULL
            CONSTRAINT DF_movimiento_caja_fecha_hora_utc DEFAULT (SYSUTCDATETIME()),
        usuario_id INT NOT NULL,
        factura INT NULL,
        pago_id BIGINT NULL,
        ingreso_caja_id INT NULL,
        retiro_caja_id INT NULL,
        referencia NVARCHAR(100) NULL,
        observacion NVARCHAR(250) NULL,
        estado NVARCHAR(20) NOT NULL
            CONSTRAINT DF_movimiento_caja_estado DEFAULT (N'Confirmado'),
        reversa_de_movimiento_id BIGINT NULL,
        correlacion_tecnica UNIQUEIDENTIFIER NULL,
        creado_utc DATETIME2(0) NOT NULL
            CONSTRAINT DF_movimiento_caja_creado_utc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_movimiento_caja_turno
            FOREIGN KEY (idTurno) REFERENCES dbo.caja_turno(idTurno),
        CONSTRAINT FK_movimiento_caja_usuario
            FOREIGN KEY (usuario_id) REFERENCES dbo.usuario(idUsuario),
        CONSTRAINT FK_movimiento_caja_ventas
            FOREIGN KEY (factura) REFERENCES dbo.ventas(factura),
        CONSTRAINT FK_movimiento_caja_pago
            FOREIGN KEY (pago_id) REFERENCES dbo.venta_pago(idPago),
        CONSTRAINT FK_movimiento_caja_ingreso
            FOREIGN KEY (ingreso_caja_id) REFERENCES dbo.ingreso_caja(idIngreso),
        CONSTRAINT FK_movimiento_caja_retiro
            FOREIGN KEY (retiro_caja_id) REFERENCES dbo.retiro_caja(idRetiro),
        CONSTRAINT FK_movimiento_caja_reversa
            FOREIGN KEY (reversa_de_movimiento_id) REFERENCES dbo.movimiento_caja(idMovimiento),
        CONSTRAINT CK_movimiento_caja_tipo
            CHECK (tipo_movimiento IN (
                N'FondoInicial',
                N'VentaEfectivo',
                N'IngresoCaja',
                N'RetiroCaja',
                N'AjustePositivo',
                N'AjusteNegativo',
                N'DevolucionEfectivo',
                N'CierreDiferencia',
                N'Reversa'
            )),
        CONSTRAINT CK_movimiento_caja_origen
            CHECK (origen IN (N'POS.Api', N'WPF', N'MigracionFutura', N'AjusteManual')),
        CONSTRAINT CK_movimiento_caja_estado
            CHECK (estado IN (N'Confirmado', N'Reversado')),
        CONSTRAINT CK_movimiento_caja_moneda
            CHECK (moneda IN ('CRC', 'USD')),
        CONSTRAINT CK_movimiento_caja_monto
            CHECK (monto > 0)
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.movimiento_caja')
      AND name = N'IX_movimiento_caja_turno_fecha'
)
BEGIN
    CREATE INDEX IX_movimiento_caja_turno_fecha
        ON dbo.movimiento_caja(idTurno, fecha_hora_utc)
        INCLUDE (tipo_movimiento, monto, estado, factura, pago_id);
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.movimiento_caja')
      AND name = N'IX_movimiento_caja_factura'
)
BEGIN
    CREATE INDEX IX_movimiento_caja_factura
        ON dbo.movimiento_caja(factura)
        INCLUDE (idTurno, tipo_movimiento, monto, estado, pago_id, fecha_hora_utc)
        WHERE factura IS NOT NULL;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.movimiento_caja')
      AND name = N'IX_movimiento_caja_pago'
)
BEGIN
    CREATE INDEX IX_movimiento_caja_pago
        ON dbo.movimiento_caja(pago_id)
        INCLUDE (idTurno, tipo_movimiento, monto, estado, factura, fecha_hora_utc)
        WHERE pago_id IS NOT NULL;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.movimiento_caja')
      AND name = N'UX_movimiento_caja_pago_efectivo'
)
BEGIN
    CREATE UNIQUE INDEX UX_movimiento_caja_pago_efectivo
        ON dbo.movimiento_caja(pago_id)
        WHERE pago_id IS NOT NULL
          AND tipo_movimiento = N'VentaEfectivo'
          AND estado = N'Confirmado';
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.movimiento_caja')
      AND name = N'IX_movimiento_caja_correlacion'
)
BEGIN
    CREATE INDEX IX_movimiento_caja_correlacion
        ON dbo.movimiento_caja(correlacion_tecnica)
        WHERE correlacion_tecnica IS NOT NULL;
END;

COMMIT TRANSACTION;
