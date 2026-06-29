# Matriz de errores idempotencia Ingreso Caja

| Caso | Resultado |
| --- | --- |
| Sin JWT | 401 |
| JWT invalido | 401 |
| Sin `Caja.Ingresar` | 403 |
| `EnableCajaApiWrite=false` | 503 |
| Ambiente no Test | 503 |
| `Idempotency-Key` ausente con escritura habilitada | 400 |
| `Idempotency-Key` invalida con escritura habilitada | 400 |
| Misma key, mismo request, completada | 200 con resultado original |
| Misma key, request distinto | 409 |
| Misma key `EnProceso` | 409 |
| Estado `Fallida` | 409 hasta politica futura |
| Turno inexistente | 409 |
| Turno no abierto | 409 |
| Error tecnico transaccional | rollback y error seguro |
| Violacion de unicidad | 409 seguro |

Las respuestas no exponen SQL, stack trace, token, key completa, request hash, connection string, usuario individual ni datos internos.
