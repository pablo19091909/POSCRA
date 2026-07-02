# Validacion de no borrado fisico en reversa WPF

Fecha UTC: 2026-07-02T01:46:29Z

## Objetivo

Confirmar que la reversa WPF por API no elimina la venta original ni sus datos relacionados, sino que registra una reversa inmutable.

## Evidencia

Venta de prueba:

- Factura: `2002`.
- Total: `10.00`.
- Metodo de pago: Efectivo.

Despues de la reversa:

- La venta original sigue existiendo en `ventas`.
- El detalle original sigue existiendo en `DetalleVenta`.
- El pago original sigue existiendo en `venta_pago`.
- Se agrego exactamente una fila en `venta_reversa`.
- Se agrego exactamente un movimiento compensatorio `Reversa`.
- La venta tiene exactamente una reversa asociada.

## Bloqueos esperados

Durante la prueba:

- La reversa fue ejecutada una sola vez.
- No se uso eliminacion fisica para la venta API en efectivo.
- No se uso `DBConnection` para ejecutar la reversa.
- No se uso `CajaHelper`.
- No se uso `RawPrinterHelper`.

## Resultado

Validacion aprobada. La reversa WPF por API preserva la venta original y agrega trazabilidad compensatoria.

