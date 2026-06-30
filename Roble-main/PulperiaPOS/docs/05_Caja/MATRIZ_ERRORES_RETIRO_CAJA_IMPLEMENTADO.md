# Matriz de errores RetiroCaja implementado

Fecha UTC: 2026-06-29 16:17:55 UTC

| Escenario | Respuesta | Escritura |
| --- | ---: | --- |
| Sin token | 401 | No |
| Token sin `Caja.Retirar` | 403 | No |
| Flag apagado | 503 | No |
| Ambiente no permitido | 503 | No |
| Key ausente con escritura habilitada | 400 | No |
| Key invalida con escritura habilitada | 400 | No |
| Body ausente | 400 | No |
| Caja invalida | 400 | No |
| Monto cero o negativo | 400 | No |
| Monto superior al disponible | 409 | No |
| Usuario inactivo | 400 | No |
| Turno inexistente | 409 | No |
| Turno no abierto | 409 | No |
| Misma key y mismo hash completado | 200 | No adicional |
| Misma key y hash distinto | 409 | No |
| Key `EnProceso` | 409 | No |
| Conflicto unico SQL | 409 | No parcial |
| Cancelacion | Propaga cancelacion | Rollback |
| Error tecnico transaccional | 503 seguro | Rollback |

## Resultado de Fase 4F.15

Con `EnableCajaApiWrite=false`:

- sin token: `401`;
- token sin permiso: `403`;
- token con permiso sin key: `503`;
- token con permiso y key invalida: `503`;
- token con permiso y key valida: `503`.

No hubo escrituras.
