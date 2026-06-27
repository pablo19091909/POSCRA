# Resultados pruebas idempotencia y rollback

Fecha/hora UTC: 2026-06-26

## Resultado

No ejecutadas.

## Motivo

No se confirmo ambiente aislado no productivo. La conexion actual no fue aceptada para pruebas de escritura porque no presenta identificacion logica no productiva y sus agregados coinciden con la base historica productiva conocida.

## Casos pendientes

| Caso | Estado |
| --- | --- |
| M - Misma IdempotencyKey, mismo request | Pendiente |
| N - Misma IdempotencyKey, request distinto | Pendiente |
| O - Doble clic/concurrencia | Pendiente |
| Rollback por stock insuficiente | Pendiente |
| Rollback por saldo insuficiente | Pendiente |
| Rollback por monto insuficiente | Pendiente |
| Rollback por producto inexistente | Pendiente |
| Rollback por cliente inexistente | Pendiente |
| Rollback por metodo no soportado | Pendiente |

## Integridad

No se ejecuto ninguna solicitud con escritura habilitada.

Las tablas de soporte permanecen sin registros:

- `venta_idempotencia`: 0.
- `venta_pago`: 0.
- `venta_auditoria`: 0.

## Requisito para ejecutar

Ejecutar estos casos unicamente en base de prueba aislada, con datos sinteticos o sanitizados, y con `EnableVentasApiWrite=true` solo en configuracion local no versionada.
