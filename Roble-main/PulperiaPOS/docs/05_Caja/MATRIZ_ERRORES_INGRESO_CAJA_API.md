# Matriz de errores Ingreso Caja API

| Caso | Resultado esperado |
| --- | --- |
| Sin JWT | 401 |
| JWT sin `Caja.Ingresar` | 403 |
| `EnableCajaApiWrite=false` | 503 seguro |
| Ambiente distinto a Test | 503 seguro |
| Marca Test ausente o no legible | 503 seguro |
| `cajaCodigo` vacio o demasiado largo | 400 |
| Turno inexistente | 409 |
| Turno `EnCierre` | 409 |
| Turno `Cerrado` | 409 |
| Monto cero o negativo | 400 |
| Monto fuera de rango | 400 |
| Motivo vacio o excesivo | 400 |
| Referencia excesiva | 400 |
| Usuario inexistente o inactivo | 400 |
| Error transaccional | rollback y 503 seguro |
| Cancelacion | sin commit parcial |
| Solicitud duplicada futura | requiere idempotencia persistente |

Las respuestas no deben exponer SQL, stack trace, host, puerto, connection strings, tokens, usuarios ni datos internos.
