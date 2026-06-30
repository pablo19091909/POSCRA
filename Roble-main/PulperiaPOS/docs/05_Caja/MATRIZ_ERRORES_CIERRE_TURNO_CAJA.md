# Matriz de errores cierre turno Caja API

Fecha UTC: 2026-06-29 21:12:41 UTC

## Seguridad HTTP actual

| Caso | Resultado actual |
| --- | ---: |
| Sin token | 401 |
| Token sin `Caja.Cerrar` | 403 |
| Token autorizado, flag apagado, sin key | 503 |
| Token autorizado, flag apagado, key invalida | 503 |
| Token autorizado, flag apagado, key valida | 503 |

Con `EnableCajaApiWrite=false`, el servicio devuelve `503` antes de validar key o body.

## Validaciones futuras con escritura permitida

| Caso | Resultado esperado |
| --- | ---: |
| Sin token | 401 |
| Token sin `Caja.Cerrar` | 403 |
| Flag apagado | 503 |
| Environment no Test o no autorizado | 503 |
| Sin `Idempotency-Key` | 400 |
| `Idempotency-Key` invalida | 400 |
| Body nulo | 400 |
| `efectivoContado < 0` | 400 |
| `rowVersion` vacia o invalida | 400 |
| Observacion > 250 caracteres | 400 |
| Diferencia != 0 sin observacion | 400 |
| Turno inexistente | 404 |
| Turno no `Abierto` | 409 |
| `rowVersion` desactualizada | 409 |
| Misma key/hash distinto | 409 |
| Idempotencia `EnProceso` | 409 |
| Cierre simultaneo perdedor | 409 |
| Error transitorio seguro | 503 |

## Pruebas puras pendientes

- diferencia cero;
- sobrante;
- faltante;
- observacion requerida cuando diferencia no es cero;
- observacion no requerida con diferencia cero;
- hash determinista;
- cambio de contado cambia hash;
- cambio de observacion cambia hash;
- cambio de rowVersion cambia hash;
- turno `EnCierre` invalido para nuevo cierre;
- turno `Cerrado` invalido;
- misma key completada devuelve resultado;
- misma key/hash distinto devuelve conflicto;
- rollback simulado antes de commit;
- ingreso/retiro bloqueado durante `EnCierre`.

## Logs

No registrar:

- `Idempotency-Key`;
- request hash;
- efectivo contado;
- diferencia;
- observacion;
- JWT;
- SQL;
- stack trace;
- connection string;
- secretos.
