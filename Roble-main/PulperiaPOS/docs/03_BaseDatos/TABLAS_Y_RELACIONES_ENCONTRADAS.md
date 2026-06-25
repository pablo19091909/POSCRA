# Tablas y Relaciones Encontradas

Fuente principal revisada:

- `C:\Users\pablo\OneDrive\Desktop\poscra database.sql`

Tambien se cruzo contra queries en codigo WPF para confirmar uso real de tablas.

## 1. Tablas encontradas en script SQL

| Tabla | Clave primaria | Uso funcional observado |
|---|---|---|
| `usuario` | `idUsuario` | Login, roles, administracion de usuarios, venta por usuario. |
| `cliente` | `idCliente` | Clientes, saldo, venta con saldo, reportes. |
| `inventario` | `idProducto` | Productos, stock, ventas, donaciones, reportes. |
| `ventas` | `factura` | Encabezado de venta, metodo de pago, voucher/comprobante, pago/vuelto. |
| `DetalleVenta` | `idDetalle` | Lineas de venta por factura/producto. |
| `ingreso_caja` | `idIngreso` | Ingresos manuales de caja. |
| `retiro_caja` | `idRetiro` | Retiros manuales de caja. |
| `cierre_caja` | `idCierre` | Cierres con totales por metodo. |
| `saldo_liberado` | `idLiberacion` | Historial de saldo de cliente liberado. |
| `TipoCambioDolar` | `fecha` | Tipo de cambio diario. |

## 2. Relaciones declaradas en script

| Relacion | Estado |
|---|---|
| `DetalleVenta.factura -> ventas.factura` | Existe FK. |
| `DetalleVenta.producto_id -> inventario.idProducto` | Existe FK. |
| `saldo_liberado.idCliente -> cliente.idCliente` | Existe FK. |
| `ventas.cliente_id -> cliente.idCliente` | Existe FK. |
| `ventas.usuario_id -> usuario.idUsuario` | Existe FK. |

## 3. Relaciones faltantes o debiles

| Area | Brecha | Impacto |
|---|---|---|
| Caja | `ingreso_caja` no tiene FK a `usuario`; guarda usuario como texto. | No hay trazabilidad fuerte. |
| Caja | `retiro_caja` no tiene usuario ni FK. | No se sabe responsable en base. |
| Caja | `cierre_caja` no tiene usuario, caja, turno ni FK. | Cierre no auditable. |
| Caja | Ventas/ingresos/retiros no se asocian a cierre. | No se puede congelar periodo cerrado. |
| Caja | No existe `CajaTurno` ni `MovimientoCaja`. | No soporta multiples turnos por dia de forma segura. |
| Ventas | No hay estado/anulado/motivoAnulacion. | Se recurre a borrado fisico. |
| Inventario | No hay tabla de movimientos de inventario. | Ajustes manuales no auditables. |
| Tipo cambio | No hay usuario/modificacion/historial. | Cambios no trazables. |

## 4. Campos relevantes por tabla

### `usuario`

Campos:

- `idUsuario int identity primary key`
- `nombre nvarchar(100) not null`
- `contrasena nvarchar(255) not null`
- `rol nvarchar(50) null`

Riesgos:

- `rol` permite NULL.
- `contrasena` almacena hash SHA-256 simple segun codigo actual.
- No hay estado, auditoria ni bloqueo.

### `cliente`

Campos:

- `idCliente`
- `nombre`
- `saldo decimal(10,2) null default 0`
- `comprobante`
- `fecha_carga_saldo datetime null`

Riesgos:

- `saldo` deberia ser NOT NULL.
- No hay historial completo de cargas/consumos de saldo.
- `fecha_carga_saldo` viene del cliente WPF.

### `inventario`

Campos:

- `idProducto nvarchar(50) primary key`
- `nombre`
- `proveedor`
- `costo decimal(10,2) null`
- `precio decimal(10,2) null`
- `stock int null`
- `vendido int null default 0`

Riesgos:

- `stock`, `precio`, `costo` permiten NULL.
- No hay constraints `stock >= 0`, `precio >= 0`.
- No hay movimientos de inventario.

### `ventas`

Campos:

- `factura identity primary key`
- `cliente_id`
- `total decimal(10,2) null`
- `fecha date null`
- `hora time(7) null`
- `usuario_id`
- `metodo_pago`
- `numero_voucher`
- `numero_comprobante`
- `monto_pagado`
- `vuelto`

Riesgos:

- Muchos campos criticos permiten NULL.
- No hay estado, anulacion, devolucion, idempotencia ni turno/caja.
- `fecha` y `hora` separados y generados por cliente.
- No guarda tipo de cambio usado para dolares.

### `DetalleVenta`

Campos:

- `idDetalle`
- `factura`
- `producto_id`
- `cantidad`
- `precio_unitario`

Riesgos:

- No hay CHECK `cantidad > 0`.
- No hay CHECK `precio_unitario >= 0`.

### `ingreso_caja`

Campos:

- `idIngreso`
- `monto decimal(10,2) not null`
- `motivo nvarchar(255) not null`
- `fecha date not null`
- `hora time(7) not null`
- `usuario nvarchar(100) not null`

Riesgos:

- Usuario como texto, no FK.
- No hay turno/caja/cierre/metodoPago/estado.
- No hay aprobacion ni anulacion.

### `retiro_caja`

Campos:

- `idRetiro`
- `monto decimal(10,2) null`
- `motivo nvarchar(255) null`
- `fecha date null`
- `hora time(7) null`

Riesgos:

- Campos criticos permiten NULL.
- No guarda usuario responsable.
- No hay turno/caja/cierre/estado.

### `cierre_caja`

Campos:

- `idCierre`
- `fecha date null`
- `total_efectivo decimal(10,2) null`
- `total_sinpe decimal(10,2) null`
- `total_datafono decimal(10,2) null`
- `observaciones nvarchar(255) null`
- `hora nvarchar(10) null`

Riesgos:

- No hay `IdTurno`, `IdCaja`, usuario, esperado, contado, diferencia.
- `hora` es texto.
- No impide cierres duplicados.
- No marca movimientos como cerrados.

### `saldo_liberado`

Campos:

- `idLiberacion`
- `idCliente`
- `monto decimal(10,2) not null`
- `fecha date not null`
- `motivo nvarchar(255) null`

Riesgos:

- No guarda usuario responsable.
- Motivo permite NULL.
- No hay aprobacion.

### `TipoCambioDolar`

Campos:

- `fecha date primary key`
- `compra real not null`
- `venta real not null`

Riesgos:

- `real` no es apropiado para valores financieros.
- No hay usuario ni historial.
- Venta no guarda tasa usada.

## 5. Indices recomendados para fase futura

No se implementan en Fase 0. Recomendaciones futuras:

- `ventas(fecha, hora)`
- `ventas(usuario_id, fecha)`
- `ventas(cliente_id, fecha)`
- `ventas(metodo_pago, fecha)`
- `DetalleVenta(factura)`
- `DetalleVenta(producto_id)`
- `ingreso_caja(fecha, hora)`
- `retiro_caja(fecha, hora)`
- `cierre_caja(fecha)`
- `cliente(nombre)`
- `inventario(nombre)`

## 6. Constraints recomendados para fase futura

No se implementan en Fase 0. Recomendaciones futuras:

- `ventas.total > 0`
- `ventas.vuelto >= 0`
- `DetalleVenta.cantidad > 0`
- `DetalleVenta.precio_unitario >= 0`
- `inventario.stock >= 0`
- `inventario.precio >= 0`
- `ingreso_caja.monto > 0`
- `retiro_caja.monto > 0`
- `TipoCambioDolar.compra > 0`
- `TipoCambioDolar.venta > 0`
- Metodo de pago controlado por catalogo o CHECK.

## 7. Modelo futuro requerido para caja

Tablas futuras sugeridas:

- `CajaTurno`
- `MovimientoCaja`
- `CierreCaja`

Regla principal:

- Una venta en efectivo aumenta caja solo por el total de venta.
- Monto recibido y vuelto son auditoria, no deben duplicar el efecto financiero.
- Tarjeta, SINPE, saldo y dolares no aumentan efectivo fisico.
- Todo evento de caja debe dejar `MovimientoCaja`.
