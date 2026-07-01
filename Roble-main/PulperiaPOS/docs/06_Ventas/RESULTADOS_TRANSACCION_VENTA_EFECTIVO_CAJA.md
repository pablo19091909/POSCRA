# Resultados transaccion venta efectivo Caja

Fecha UTC: 2026-07-01 03:27:11 UTC

## Flujo ejecutado

La operacion se ejecuto por cliente HTTP autorizado contra POS.Api:

1. Apertura de turno Test.
2. Venta API con metodo de pago `Efectivo`.
3. Insercion de detalle.
4. Insercion de pago.
5. Descuento de inventario.
6. Insercion de auditoria.
7. Insercion de movimiento `VentaEfectivo`.

No se uso WPF y no hubo fallback SQL.

## Resultados HTTP

- Apertura de turno: 200.
- Venta efectiva: 200.
- Reintento idempotente: 200.
- Conflicto por misma llave con intencion distinta: 409.
- Rollback controlado por solicitud invalida: 400.

## Resultado financiero

- Fondo inicial: 1000.00.
- Venta efectivo: 10.00.
- Efectivo esperado por pre-cierre: 1010.00.

Resumen de pre-cierre:

- `FondoInicial`: cantidad 1, total 1000.00.
- `VentaEfectivo`: cantidad 1, total 10.00.

## Movimientos del turno

- `FondoInicial`: 1.
- `VentaEfectivo`: 1.
- `IngresoCaja`: 0.
- `RetiroCaja`: 0.
- Ajustes: 0.
- Reversas: 0.
- `CierreDiferencia`: 0.

## Tablas historicas

No hubo variacion en:

- `ingreso_caja`.
- `retiro_caja`.
- `cierre_caja`.

## Confirmaciones

- No se escribio `caja_idempotencia` para la venta.
- La unica variacion en `caja_idempotencia` corresponde a la apertura del turno.
- La venta y el movimiento `VentaEfectivo` quedaron dentro de la operacion API controlada.
- El turno final permanece abierto.
