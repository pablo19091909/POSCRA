# Idempotencia y relacion Venta - VentaEfectivo

## Fuente de idempotencia

La fuente principal es `venta_idempotencia`.

No se crea una `caja_idempotencia` separada para la intencion de venta efectiva. La venta y el movimiento de caja viven en la misma transaccion y se recuperan por la idempotencia de venta.

## Reintentos

- Misma key y mismo hash: devuelve la venta completada existente.
- Misma key y hash distinto: conflicto seguro.
- Estado `EnProceso`: conflicto de solicitud en proceso.
- Estado `Fallida` sin factura: puede reintentarse tras reset controlado.

## Relacion fisica

`movimiento_caja` ya dispone de:

- `factura` -> `ventas.factura`.
- `pago_id` -> `venta_pago.idPago`.
- `UX_movimiento_caja_pago_efectivo`, unico filtrado por `pago_id`, `VentaEfectivo` y `Confirmado`.

Esto impide duplicar el movimiento confirmado para el mismo pago efectivo.

## Regla objetivo

```text
venta efectiva confirmada
<-> venta_pago efectivo registrado
<-> un unico movimiento_caja VentaEfectivo confirmado
```

## Pendiente antes de produccion

Definir si debe agregarse una constraint adicional por `factura` para escenarios futuros de multiples pagos efectivo por venta. El API actual crea un solo `venta_pago` por venta.
