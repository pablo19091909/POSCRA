# Definiciones financieras - ventas netas y efectivo neto

## Ventas brutas

`VentasBrutas` es la suma de ventas confirmadas antes de restar reversas.

Regla:

- Incluir ventas existentes.
- No ocultar ventas reversadas del historial.
- Etiquetar origen: historico SQL o Venta API.

## Reversas

`MontoReversado` es la suma de ventas con reversa valida en `venta_reversa`.

Regla:

- Una reversa valida requiere registro `venta_reversa` confirmado.
- Para efectivo, tambien debe existir movimiento compensatorio `movimiento_caja` tipo `Reversa` confirmado.
- El monto reversado se presenta una sola vez.

## Ventas netas

`VentasNetas = VentasBrutas - MontoReversado`.

Una venta reversada:

- sigue visible;
- estado visible `Reversada`;
- impacto neto `0.00`;
- no infla venta neta.

## Efectivo por ventas

`EfectivoVentasBruto = SUM(movimiento_caja.monto WHERE tipo_movimiento = VentaEfectivo AND estado = Confirmado)`.

`ReversasEfectivo = SUM(movimiento_caja.monto WHERE tipo_movimiento = Reversa AND estado = Confirmado)`.

`EfectivoVentasNeto = EfectivoVentasBruto - ReversasEfectivo`.

El signo financiero se define por `tipo_movimiento`, no por monto negativo.

## Efectivo esperado de turno

Regla oficial para reporterÃ­a:

`FondoInicial + IngresoCaja + VentaEfectivo - RetiroCaja - Reversa`.

`CierreDiferencia` se reporta como diferencia de cierre, pero no altera efectivo esperado.

## Inventario

Venta reversada:

- venta descuenta inventario;
- reversa restaura inventario;
- efecto neto esperado: cero para los productos de la venta.

## Historicos

Todo reporte combinado debe etiquetar origen:

- `Historico SQL`.
- `Venta API`.
- `Caja API`.

No se debe presentar una mezcla sin regla explicita.


