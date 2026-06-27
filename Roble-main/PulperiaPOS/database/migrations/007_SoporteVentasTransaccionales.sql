/*
    Fase 4B.1 - Soporte aditivo para ventas transaccionales por API.

    IMPORTANTE:
    - NO ejecutar sin aprobacion explicita posterior.
    - Script preparado, no aplicado en Fase 4B.1.
    - Aditivo e idempotente para SQL Server.
    - No modifica dbo.ventas, dbo.DetalleVenta, dbo.inventario ni dbo.cliente.
    - No migra datos historicos ni crea ventas.
    - No crea CajaTurno ni MovimientoCaja.
*/

SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.ventas', N'U') IS NULL
    THROW 51000, 'No existe la tabla dbo.ventas.', 1;

IF OBJECT_ID(N'dbo.DetalleVenta', N'U') IS NULL
    THROW 51001, 'No existe la tabla dbo.DetalleVenta.', 1;

IF OBJECT_ID(N'dbo.usuario', N'U') IS NULL
    THROW 51002, 'No existe la tabla dbo.usuario.', 1;

BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.venta_idempotencia', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.venta_idempotencia
    (
        idIdempotencia BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_venta_idempotencia PRIMARY KEY,
        idempotency_key UNIQUEIDENTIFIER NOT NULL,
        usuario_id INT NOT NULL,
        request_hash VARBINARY(32) NOT NULL,
        estado NVARCHAR(20) NOT NULL
            CONSTRAINT DF_venta_idempotencia_estado DEFAULT (N'EnProceso'),
        factura INT NULL,
        response_hash VARBINARY(32) NULL,
        error_code NVARCHAR(80) NULL,
        creado_utc DATETIME2(0) NOT NULL
            CONSTRAINT DF_venta_idempotencia_creado_utc DEFAULT (SYSUTCDATETIME()),
        actualizado_utc DATETIME2(0) NOT NULL
            CONSTRAINT DF_venta_idempotencia_actualizado_utc DEFAULT (SYSUTCDATETIME()),
        expira_utc DATETIME2(0) NULL,
        observaciones NVARCHAR(250) NULL,
        CONSTRAINT FK_venta_idempotencia_usuario
            FOREIGN KEY (usuario_id) REFERENCES dbo.usuario(idUsuario),
        CONSTRAINT FK_venta_idempotencia_ventas
            FOREIGN KEY (factura) REFERENCES dbo.ventas(factura),
        CONSTRAINT CK_venta_idempotencia_estado
            CHECK (estado IN (N'EnProceso', N'Completada', N'Fallida')),
        CONSTRAINT CK_venta_idempotencia_expira
            CHECK (expira_utc IS NULL OR expira_utc > creado_utc)
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.venta_idempotencia')
      AND name = N'UX_venta_idempotencia_usuario_key'
)
BEGIN
    CREATE UNIQUE INDEX UX_venta_idempotencia_usuario_key
        ON dbo.venta_idempotencia(usuario_id, idempotency_key);
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.venta_idempotencia')
      AND name = N'IX_venta_idempotencia_estado_expira'
)
BEGIN
    CREATE INDEX IX_venta_idempotencia_estado_expira
        ON dbo.venta_idempotencia(estado, expira_utc)
        INCLUDE (usuario_id, factura, actualizado_utc);
END;

IF OBJECT_ID(N'dbo.venta_pago', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.venta_pago
    (
        idPago BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_venta_pago PRIMARY KEY,
        factura INT NOT NULL,
        metodo_pago NVARCHAR(30) NOT NULL,
        moneda CHAR(3) NOT NULL
            CONSTRAINT DF_venta_pago_moneda DEFAULT ('CRC'),
        monto DECIMAL(18,2) NOT NULL,
        monto_recibido DECIMAL(18,2) NULL,
        vuelto DECIMAL(18,2) NULL,
        tipo_cambio_aplicado DECIMAL(18,6) NULL,
        referencia NVARCHAR(100) NULL,
        voucher NVARCHAR(100) NULL,
        fecha_hora_utc DATETIME2(0) NOT NULL
            CONSTRAINT DF_venta_pago_fecha_hora_utc DEFAULT (SYSUTCDATETIME()),
        usuario_id INT NULL,
        estado NVARCHAR(20) NOT NULL
            CONSTRAINT DF_venta_pago_estado DEFAULT (N'Registrado'),
        observaciones NVARCHAR(250) NULL,
        CONSTRAINT FK_venta_pago_ventas
            FOREIGN KEY (factura) REFERENCES dbo.ventas(factura),
        CONSTRAINT FK_venta_pago_usuario
            FOREIGN KEY (usuario_id) REFERENCES dbo.usuario(idUsuario),
        CONSTRAINT CK_venta_pago_metodo
            CHECK (metodo_pago IN (N'Efectivo', N'Tarjeta', N'Sinpe', N'Dolares', N'SaldoCliente', N'Donación')),
        CONSTRAINT CK_venta_pago_moneda
            CHECK (moneda IN ('CRC', 'USD')),
        CONSTRAINT CK_venta_pago_estado
            CHECK (estado IN (N'Registrado', N'Anulado', N'Devuelto')),
        CONSTRAINT CK_venta_pago_montos
            CHECK (
                monto > 0
                AND (monto_recibido IS NULL OR monto_recibido >= 0)
                AND (vuelto IS NULL OR vuelto >= 0)
            ),
        CONSTRAINT CK_venta_pago_tipo_cambio
            CHECK (tipo_cambio_aplicado IS NULL OR tipo_cambio_aplicado > 0)
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.venta_pago')
      AND name = N'IX_venta_pago_factura'
)
BEGIN
    CREATE INDEX IX_venta_pago_factura
        ON dbo.venta_pago(factura, estado);
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.venta_pago')
      AND name = N'IX_venta_pago_metodo_fecha'
)
BEGIN
    CREATE INDEX IX_venta_pago_metodo_fecha
        ON dbo.venta_pago(metodo_pago, fecha_hora_utc)
        INCLUDE (factura, monto, moneda, estado);
END;

IF OBJECT_ID(N'dbo.venta_auditoria', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.venta_auditoria
    (
        idAuditoria BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_venta_auditoria PRIMARY KEY,
        factura INT NULL,
        idIdempotencia BIGINT NULL,
        evento NVARCHAR(50) NOT NULL,
        usuario_id INT NULL,
        fecha_hora_utc DATETIME2(0) NOT NULL
            CONSTRAINT DF_venta_auditoria_fecha_hora_utc DEFAULT (SYSUTCDATETIME()),
        origen NVARCHAR(50) NOT NULL
            CONSTRAINT DF_venta_auditoria_origen DEFAULT (N'POS.Api'),
        trace_id NVARCHAR(100) NULL,
        detalle_json NVARCHAR(MAX) NULL,
        observaciones NVARCHAR(250) NULL,
        CONSTRAINT FK_venta_auditoria_ventas
            FOREIGN KEY (factura) REFERENCES dbo.ventas(factura),
        CONSTRAINT FK_venta_auditoria_idempotencia
            FOREIGN KEY (idIdempotencia) REFERENCES dbo.venta_idempotencia(idIdempotencia),
        CONSTRAINT FK_venta_auditoria_usuario
            FOREIGN KEY (usuario_id) REFERENCES dbo.usuario(idUsuario),
        CONSTRAINT CK_venta_auditoria_evento
            CHECK (evento IN (
                N'VentaCreada',
                N'VentaAnulada',
                N'VentaDevuelta',
                N'PagoRegistrado',
                N'ErrorDeProcesamiento',
                N'AjusteAutorizado'
            )),
        CONSTRAINT CK_venta_auditoria_json
            CHECK (detalle_json IS NULL OR ISJSON(detalle_json) = 1)
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.venta_auditoria')
      AND name = N'IX_venta_auditoria_factura_fecha'
)
BEGIN
    CREATE INDEX IX_venta_auditoria_factura_fecha
        ON dbo.venta_auditoria(factura, fecha_hora_utc)
        INCLUDE (evento, usuario_id);
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.venta_auditoria')
      AND name = N'IX_venta_auditoria_evento_fecha'
)
BEGIN
    CREATE INDEX IX_venta_auditoria_evento_fecha
        ON dbo.venta_auditoria(evento, fecha_hora_utc)
        INCLUDE (factura, usuario_id);
END;

COMMIT TRANSACTION;
