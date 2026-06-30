# Resultados de idempotencia - Ingreso Caja API

Fecha UTC: 2026-06-29 15:07:18 UTC

## Resultado funcional

| Escenario | Resultado | Escritura nueva |
| --- | --- | --- |
| Primera solicitud `500.00` | HTTP `200` | 1 movimiento + 1 idempotencia |
| Misma key + mismo request | HTTP `200` | Ninguna adicional |
| Misma key + request distinto | HTTP `409` | Ninguna |
| Concurrencia misma key `1.00` | HTTP `200` y `200` | 1 movimiento + 1 idempotencia |

## Estado final agregado

- Movimientos `IngresoCaja`: `2`.
- Idempotencias totales: `2`.
- Idempotencias `Completada`: `2`.
- Idempotencias `EnProceso`: `0`.
- Idempotencias `Fallida`: `0`.
- Duplicados por key/usuario/operacion: `0`.
- Idempotencias completadas sin movimiento: `0`.
- Movimientos `IngresoCaja` sin idempotencia completada: `0`.
- Efectivo esperado: `1501.00`.

## Confirmaciones de seguridad

- No se imprimieron tokens.
- No se imprimieron idempotency keys.
- No se imprimieron hashes.
- No se imprimieron credenciales ni connection strings.
- No se modificaron usuarios, roles ni permisos.
- No se modifico WPF.

## Confirmaciones de integridad

- `ingreso_caja` historico no cambio.
- `retiro_caja` historico no cambio.
- `cierre_caja` historico no cambio.
- Ventas y pagos no cambiaron.
- Inventario agregado no cambio.
- Saldo agregado de clientes no cambio.
