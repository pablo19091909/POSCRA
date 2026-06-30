# Resultados de idempotencia y concurrencia RetiroCaja

Fecha UTC: 2026-06-29 16:43:03 UTC

## Escenarios HTTP

| Escenario | Resultado | Escritura |
| --- | ---: | --- |
| Retiro principal `500.00` | 200 | 1 movimiento + 1 idempotencia |
| Misma key + mismo request | 200 | Ninguna adicional |
| Misma key + request distinto | 409 | Ninguna |
| Retiro mayor al disponible | 409 | Ninguna |
| Concurrencia `800.00` + `800.00` | 200 y 409 | 1 movimiento + 1 idempotencia |

## Integridad de idempotencia

- Idempotencias `RetiroCaja`: `2`.
- Idempotencias `RetiroCaja Completada`: `2`.
- Idempotencias `RetiroCaja EnProceso`: `0`.
- Idempotencias `RetiroCaja Fallida`: `0`.
- Duplicados por usuario + operacion + key: `0`.
- Idempotencias completadas sin movimiento: `0`.
- Retiros sin idempotencia completada: `0`.

## Integridad de movimientos

- Retiros API totales: `2`.
- Retiro principal de `500.00`: `1`.
- Retiro concurrente de `800.00`: `1`.
- Retiros por monto insuficiente: `0`.
- Retiros con relaciones historicas no esperadas: `0`.
- Retiros con monto invalido: `0`.
- Retiros con moneda invalida: `0`.

## Pre-cierre

```text
Inicial: 1501.00
Despues del retiro principal: 1001.00
Despues de concurrencia exitosa: 201.00
```

El calculo se mantiene basado solo en `movimiento_caja`.

## Conclusiones

La repeticion con misma key no duplico retiro. La misma key con intencion distinta fue rechazada. El intento insuficiente no persistio datos. La concurrencia con keys distintas permitio como maximo una operacion exitosa y evito efectivo negativo.
