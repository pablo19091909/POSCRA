# Diseno de auditoria de ventas

Fecha UTC: 2026-06-26 10:54:47 UTC

## Decision

Se recomienda tabla especifica `venta_auditoria` para esta etapa.

Justificacion:

- es mas simple que una auditoria global;
- concentra eventos de venta, pagos, anulaciones y devoluciones;
- facilita reportes y soporte;
- evita introducir un sistema general antes de estabilizar ventas API.

## Tabla propuesta

`venta_auditoria`

Campos principales:

- `idAuditoria`: PK.
- `factura`: FK nullable a `ventas`.
- `idIdempotencia`: FK nullable a `venta_idempotencia`.
- `evento`: tipo de evento.
- `usuario_id`: FK nullable a `usuario`.
- `fecha_hora_utc`.
- `origen`: por ejemplo `POS.Api`.
- `trace_id`: correlacion segura.
- `detalle_json`: JSON tecnico, sin secretos.
- `observaciones`.

## Eventos propuestos

- `VentaCreada`
- `VentaAnulada`
- `VentaDevuelta`
- `PagoRegistrado`
- `ErrorDeProcesamiento`
- `AjusteAutorizado`

## Datos antes/despues

Guardar datos antes/despues solo cuando sean necesarios para anulacion, devolucion o ajuste autorizado.

No guardar:

- contrasenas;
- tokens;
- connection strings;
- hashes sensibles;
- datos personales innecesarios;
- payload completo si un hash o resumen es suficiente.

## Relacion futura

`venta_auditoria` servira para:

- confirmar creacion por API;
- rastrear errores transaccionales;
- auditar anulaciones;
- auditar devoluciones;
- relacionar eventos con idempotencia;
- relacionar movimientos de caja cuando exista `MovimientoCaja`.

## Desempeno

Indices propuestos:

- por `factura` y `fecha_hora_utc`;
- por `evento` y `fecha_hora_utc`.

Esto cubre consultas de soporte por venta y monitoreo operativo por evento.
