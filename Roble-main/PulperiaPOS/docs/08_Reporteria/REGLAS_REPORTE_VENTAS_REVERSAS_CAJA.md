# Reglas de reporte - ventas, reversas y caja

## Reglas de ventas

- Mostrar venta original aunque este reversada.
- Estado financiero permitido: `Confirmada` o `Reversada`.
- Venta reversada contribuye a bruto, no a neto.
- Venta API se identifica cuando existe pago API relacionado.
- Venta historica SQL se mantiene como origen separado.

## Reglas de pagos

- Pago efectivo registrado suma a bruto solo si no fue compensado por reversa.
- Pago original no se elimina tras reversa.
- El reporte no debe asumir anulacion fisica de pago.

## Reglas de reversas

- Reversa valida: `venta_reversa.estado = Confirmada`.
- Reversa de efectivo consistente: tiene movimiento compensatorio `Reversa` confirmado.
- Doble reversa para la misma venta es alerta.
- Movimiento `Reversa` sin `venta_reversa` valida es alerta.

## Reglas de caja

- `VentaEfectivo` aumenta efectivo bruto.
- `Reversa` reduce efectivo neto.
- `IngresoCaja` y `RetiroCaja` son movimientos manuales, no ventas.
- `CierreDiferencia` se reporta separado.
- `cierre_caja` historico no se mezcla con `caja_turno` API sin etiqueta.

## Reglas de filtros

Filtros preparados:

- rango UTC;
- paginacion en detalle;
- origen;
- metodo de pago;
- estado financiero;
- caja/turno en fase posterior de WPF.

## Contratos implementados

- Resumen de ventas.
- Detalle de ventas.
- Reversas.
- Resumen de caja.
- Turnos.
- Movimientos.
- Inconsistencias.


