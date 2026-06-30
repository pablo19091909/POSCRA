# Matriz errores cierre turno implementado

Fecha UTC: 2026-06-29 21:37:22 UTC

## Validado con flag apagado

| Caso | Resultado |
| --- | ---: |
| Sin token | 401 |
| Token sin `Caja.Cerrar` | 403 |
| Autorizado, sin key, flag apagado | 503 |
| Autorizado, key invalida, flag apagado | 503 |
| Autorizado, key valida, flag apagado | 503 |

## Esperado con escritura habilitada

| Caso | Resultado |
| --- | ---: |
| Body nulo | 400 |
| `efectivoContado < 0` | 400 |
| `rowVersion` ausente o invalida | 400 |
| `Idempotency-Key` ausente o invalida | 400 |
| Observacion mayor al limite | 400 |
| Diferencia sin observacion | 400 |
| Turno inexistente | 404 |
| Turno no `Abierto` | 409 |
| `rowVersion` desactualizada | 409 |
| Key `EnProceso` | 409 |
| Misma key/hash distinto | 409 |
| Solicitud duplicada por constraint | 409 |
| Ambiente o flag no permitido | 503 |

## Integridad esperada

- No cerrar sin idempotencia completada.
- No completar idempotencia sin cierre real.
- No dejar turno `EnCierre` despues de rollback.
- No crear `CierreDiferencia` sin cierre.
- No modificar `cierre_caja`, `ingreso_caja` ni `retiro_caja`.
- No modificar WPF.
