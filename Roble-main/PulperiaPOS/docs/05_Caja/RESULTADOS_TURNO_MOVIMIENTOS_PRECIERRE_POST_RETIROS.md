# Resultados turno, movimientos y pre-cierre post retiros

Fecha UTC: 2026-06-29 21:02:52 UTC

## Turno

| Validacion | Resultado |
| --- | ---: |
| Environment Test autorizado | 1 |
| Turno abierto para caja Test | 1 |
| Turnos abiertos o en cierre para caja Test | 1 |
| Turnos en cierre o cerrados para caja Test | 0 |
| Fondo inicial | 1000.00 |
| Campos de cierre nulos | 1 |
| Usuario apertura valido | 1 |
| `row_version` cambio por lecturas | No |

## Movimientos

| Tipo | Cantidad | Total |
| --- | ---: | ---: |
| FondoInicial | 1 | 1000.00 |
| IngresoCaja | 2 | 501.00 |
| RetiroCaja | 2 | 1300.00 |
| VentaEfectivo | 0 | 0.00 |
| AjustePositivo | 0 | 0.00 |
| AjusteNegativo | 0 | 0.00 |
| DevolucionEfectivo | 0 | 0.00 |
| CierreDiferencia | 0 | 0.00 |
| Reversa | 0 | 0.00 |

Validaciones:

- movimientos del turno: `5`;
- montos positivos: `5`;
- monedas invalidas: `0`;
- fechas UTC presentes: `5`;
- relaciones historicas inesperadas: `0`;
- movimientos huerfanos: `0`;
- duplicados por firma operativa: `0`.

## Pre-cierre

| Concepto | Total |
| --- | ---: |
| FondoInicial | 1000.00 |
| IngresosCaja | 501.00 |
| RetirosCaja | 1300.00 |
| VentasEfectivo | 0.00 |
| Ajustes | 0.00 |
| Devoluciones | 0.00 |
| DiferenciaCierre | 0.00 |
| EfectivoEsperado | 201.00 |

El pre-cierre fue validado por API con HTTP `200` y coincide con los agregados directos de `movimiento_caja`.

## Linea base antes/despues

| Agregado | Antes | Despues |
| --- | ---: | ---: |
| caja_turno | 1 | 1 |
| movimiento_caja | 5 | 5 |
| caja_idempotencia | 4 | 4 |
| ingreso_caja | 9 | 9 |
| retiro_caja | 6 | 6 |
| cierre_caja | 15 | 15 |
| ventas | 1948 | 1948 |
| venta_pago | 10 | 10 |
| venta_idempotencia | 10 | 10 |
| inventario agregado | 3296.00 | 3296.00 |
| saldo clientes agregado | -2957962.50 | -2957962.50 |
| Cliente General | 1 | 1 |

No hubo escrituras durante la validacion.
