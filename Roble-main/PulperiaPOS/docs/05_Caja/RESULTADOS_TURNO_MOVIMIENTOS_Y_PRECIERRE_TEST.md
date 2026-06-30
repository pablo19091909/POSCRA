# Resultados de turno, movimientos y pre-cierre Test

Fecha UTC: 2026-06-29 15:56:11 UTC

## Turno

- Caja: `CAJA_PRINCIPAL_TEST`.
- Estado: `Abierto`.
- Turnos abiertos o en cierre para la caja: `1`.
- Fondo inicial: `1000.00`.
- Sin cierre registrado.
- Sin cambio de `row_version` por lecturas.

## Movimientos del turno

| Tipo | Cantidad | Total |
| --- | ---: | ---: |
| FondoInicial | 1 | 1000.00 |
| IngresoCaja | 2 | 501.00 |
| RetiroCaja | 0 | 0.00 |
| VentaEfectivo | 0 | 0.00 |
| AjustePositivo | 0 | 0.00 |
| AjusteNegativo | 0 | 0.00 |
| DevolucionEfectivo | 0 | 0.00 |
| CierreDiferencia | 0 | 0.00 |
| Reversa | 0 | 0.00 |

Total de movimientos: `3`.

## Validaciones de movimientos

- Montos no positivos: `0`.
- Monedas invalidas: `0`.
- Fechas UTC faltantes: `0`.
- Relaciones historicas no esperadas: `0`.
- Movimientos huerfanos: `0`.

## Pre-cierre

El endpoint de pre-cierre respondio `200`.

Calculo validado:

```text
1000.00 + 501.00 = 1501.00
```

Resultado:

- efectivo esperado: `1501.00`;
- calculado desde `movimiento_caja`;
- sin dependencia de tablas historicas;
- sin escritura de datos.
