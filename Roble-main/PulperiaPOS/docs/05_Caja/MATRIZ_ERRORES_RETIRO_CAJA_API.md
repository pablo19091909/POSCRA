# Matriz de errores RetiroCaja API

Fecha UTC: 2026-06-29 16:07:09 UTC

| Escenario | Estado esperado | Escritura |
| --- | ---: | --- |
| Sin token | 401 | No |
| Token sin `Caja.Retirar` | 403 | No |
| Flag apagado | 503 | No |
| Ambiente no Test | 503 | No |
| `Idempotency-Key` ausente | 400 futuro, 503 con flag apagado | No |
| `Idempotency-Key` invalida | 400 futuro, 503 con flag apagado | No |
| Body ausente | 400 futuro | No |
| Caja invalida | 400 futuro | No |
| Monto cero o negativo | 400 futuro | No |
| Motivo invalido | 400 futuro | No |
| Turno inexistente | 409 futuro | No |
| Turno `EnCierre` o `Cerrado` | 409 futuro | No |
| Monto mayor a disponible | 409 futuro | No |
| Misma key + mismo request | 200 futuro repetido | No adicional |
| Misma key + request distinto | 409 futuro | No |
| Key `EnProceso` | 409 futuro | No |
| Error antes de commit | 503 seguro futuro | Rollback |

## Pruebas ejecutadas en Fase 4F.14

Con `EnableCajaApiWrite=false`:

- sin token: `401`;
- token sin permiso: `403`;
- token con permiso sin key: `503`;
- token con permiso y key invalida: `503`;
- token con permiso y key valida: `503`.

No se crearon retiros, movimientos ni idempotencias.
