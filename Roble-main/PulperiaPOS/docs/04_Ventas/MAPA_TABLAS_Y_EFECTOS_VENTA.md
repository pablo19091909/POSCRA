# Mapa de tablas y efectos de venta

Fecha UTC: 2026-06-26 10:45:52 UTC

## Diagrama textual

```text
usuario(idUsuario)
  -> ventas.usuario_id

cliente(idCliente)
  -> ventas.cliente_id
  -> saldo_liberado.idCliente

ventas(factura)
  -> DetalleVenta.factura

inventario(idProducto)
  -> DetalleVenta.producto_id

ventas
  -> CajaHelper calcula efectivo/SINPE/datafono
  -> cierre_caja guarda agregados

ingreso_caja/retiro_caja
  -> CajaHelper calcula efectivo disponible
```

## Tablas reales

### ventas

- PK: `factura`.
- FK: `cliente_id -> cliente.idCliente`, `usuario_id -> usuario.idUsuario`.
- Campos relevantes: `cliente_id`, `total`, `fecha`, `hora`, `usuario_id`, `metodo_pago`, `numero_voucher`, `numero_comprobante`, `monto_pagado`, `vuelto`.
- Campos nulos peligrosos: todos salvo `factura` son nullable.
- Riesgos: encabezado puede tener total nulo, cliente nulo, usuario nulo, metodo nulo, pago nulo o vuelto nulo.

### DetalleVenta

- PK: `idDetalle`.
- FK: `factura -> ventas.factura`, `producto_id -> inventario.idProducto`.
- Campos relevantes: `factura`, `producto_id`, `cantidad`, `precio_unitario`.
- Riesgos: no se observo constraint de cantidad positiva ni precio positivo; depende de WPF.

### inventario

- PK: `idProducto`.
- Campos relevantes para ventas: `idProducto`, `nombre`, `precio`, `stock`, `vendido`.
- Campos administrativos no necesarios en venta API: `proveedor`, `costo`.
- Riesgos: `precio`, `stock`, `vendido` son nullable; update de stock no esta condicionado por disponibilidad.

### cliente

- PK: `idCliente`.
- Campos relevantes: `nombre`, `saldo`, `comprobante`, `fecha_carga_saldo`.
- Riesgos: `saldo` nullable y puede ser negativo; no hay limite de credito ni historial completo de consumos de saldo.

### usuario

- PK: `idUsuario`.
- Campos relevantes: `nombre`, `rol`, `activo`.
- Riesgos: venta actual usa `UserSession.IdUsuario`; el servidor futuro debe validar token, usuario activo y permiso.

### ingreso_caja

- PK: `idIngreso`.
- Campos: `monto`, `motivo`, `fecha`, `hora`, `usuario`.
- Riesgos: usuario se guarda como texto, no FK; no relaciona turno.

### retiro_caja

- PK: `idRetiro`.
- Campos: `monto`, `motivo`, `fecha`, `hora`.
- Riesgos: no registra usuario como columna; campos importantes son nullable.

### cierre_caja

- PK: `idCierre`.
- Campos: `fecha`, `hora`, `total_efectivo`, `total_sinpe`, `total_datafono`, `observaciones`.
- Riesgos: no hay turno, estado, usuario responsable ni bloqueo de ventas posteriores.

### saldo_liberado

- PK: `idLiberacion`.
- FK: `idCliente -> cliente.idCliente`.
- Campos: `idCliente`, `monto`, `fecha`, `motivo`.
- Riesgos: se crea desde WPF si no existe en flujo de cliente; no es parte transaccional de venta.

### TipoCambioDolar

- PK: `fecha`.
- Campos: `compra`, `venta`.
- Riesgos: usa `real`; la venta en dolares guarda monto convertido pero no monto original ni tipo de cambio aplicado.

## Efectos directos de venta

| Tabla | Operacion actual | Momento | Observacion |
|---|---|---|---|
| `ventas` | INSERT | inicio de transaccion | total calculado en WPF |
| `DetalleVenta` | INSERT por item | dentro de transaccion | precio unitario viene del carrito |
| `inventario` | UPDATE por item | dentro de transaccion | `stock = stock - cantidad`, `vendido = vendido + cantidad` |
| `cliente` | UPDATE | dentro de transaccion si cliente no general | descuenta todo el total aunque el metodo visual sea saldo cliente |
| `ingreso_caja` | ninguno | no aplica | ventas no crean movimiento de caja |
| `retiro_caja` | ninguno | no aplica | no participa en venta |
| `cierre_caja` | ninguno | no aplica | se calcula despues por agregados |

## Campos faltantes recomendados

- Idempotency key en venta o tabla de solicitudes.
- Estado de venta: creada, anulada, devuelta.
- Fecha/hora UTC confiable.
- Usuario/cajero obligatorio y no nullable.
- Caja/turno obligatorio cuando exista.
- Tabla de pagos por venta.
- Auditoria de cambios/anulaciones.
- Snapshot de tipo de cambio usado.
- Motivo y usuario de anulacion/devolucion.
