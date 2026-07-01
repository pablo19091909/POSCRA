# Validacion idempotencia venta efectivo API

Fecha UTC: 2026-07-01 03:27:11 UTC

## Pruebas ejecutadas

Se ejecuto una venta unica con una llave de idempotencia nueva generada para la fase.

No se documenta ni se imprime la llave.

## Venta inicial

- HTTP: 200.
- Resultado: nueva.
- Registros creados:
  - `ventas`: +1.
  - `DetalleVenta`: +1.
  - `venta_pago`: +1.
  - `venta_idempotencia`: +1.
  - `venta_auditoria`: +1.
  - `movimiento_caja`: +1 `VentaEfectivo`.

## Reintento con misma llave y mismo cuerpo

- HTTP: 200.
- Resultado: repetida.
- No duplico venta.
- No duplico detalle.
- No duplico pago.
- No duplico auditoria.
- No duplico movimiento `VentaEfectivo`.
- No desconto inventario por segunda vez.

## Misma llave con intencion distinta

- HTTP: 409.
- No creo una segunda venta.
- No creo pago adicional.
- No creo movimiento de caja adicional.
- No modifico inventario.

## Rollback controlado

Se envio una solicitud invalida controlada con una llave nueva.

- HTTP: 400.
- La transaccion fue rechazada.
- No quedo idempotencia adicional.
- No quedo venta parcial.
- No quedo detalle parcial.
- No quedo pago parcial.
- No quedo movimiento de caja parcial.

## Estados pendientes

Posterior a la fase:

- `caja_idempotencia` en proceso: 0.
- `caja_idempotencia` fallida: 0.
- `venta_idempotencia` en proceso: 0.
- `venta_idempotencia` fallida: 0.

## Conclusion

La idempotencia de venta efectiva quedo validada para el caso feliz, reintento identico, conflicto de intencion y rollback controlado.
