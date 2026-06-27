# Diseno de pagos de venta

Fecha UTC: 2026-06-26 10:54:47 UTC

## Objetivo

Registrar pagos de venta de forma normalizada para soportar el metodo principal actual y pagos multiples en el futuro, sin cambiar todavia el comportamiento de WPF.

## Tabla propuesta

`venta_pago`

Campos principales:

- `idPago`: PK.
- `factura`: FK a `ventas`.
- `metodo_pago`: `Efectivo`, `Tarjeta`, `Sinpe`, `Dolares`, `SaldoCliente`.
- `moneda`: `CRC` o `USD`.
- `monto`: monto aplicado a la venta.
- `monto_recibido`: monto entregado por el cliente cuando aplica.
- `vuelto`: vuelto calculado por API.
- `tipo_cambio_aplicado`: requerido para dolares cuando aplique.
- `referencia`: comprobante, referencia SINPE u otra referencia externa.
- `voucher`: voucher de tarjeta si aplica.
- `fecha_hora_utc`.
- `usuario_id`: FK nullable a `usuario`.
- `estado`: `Registrado`, `Anulado`, `Devuelto`.
- `observaciones`.

## Reglas por metodo

| Metodo | Regla futura |
|---|---|
| Efectivo | `monto` representa el total aplicado; `monto_recibido` y `vuelto` son auditoria. |
| Tarjeta | requiere voucher o referencia; no aumenta efectivo fisico. |
| Sinpe | requiere referencia/comprobante; no aumenta efectivo fisico. |
| Dolares | guarda moneda USD, tipo de cambio aplicado y monto equivalente. |
| SaldoCliente | registra consumo de saldo; no aumenta efectivo fisico. |

## Decision sobre caja

No se crea `MovimientoCaja` en esta fase. `venta_pago` deja suficiente trazabilidad para que una fase posterior cree movimientos de caja derivados o relacionados.

## Compatibilidad con pagos combinados

Aunque WPF actual usa un metodo principal, `venta_pago` permite varios registros por `factura`. La futura API podra validar que la suma de pagos cubre el total sin cambiar la tabla.

## Constraints propuestos

- FK obligatoria a `ventas`.
- CHECK de metodo.
- CHECK de moneda.
- CHECK de estado.
- CHECK de montos no negativos.
- CHECK de tipo de cambio positivo cuando exista.

## No duplicar fuente actual

Las columnas actuales de `ventas` se mantienen por compatibilidad y reportes actuales. La futura API podra poblar ambas: columnas legacy para compatibilidad y `venta_pago` para trazabilidad formal.
