# Matriz de errores de apertura de turno Caja API

## Seguridad

| Caso | Resultado |
| --- | --- |
| Sin JWT | 401 |
| JWT sin `Caja.Abrir` | 403 |
| Flag apagado | 503 seguro |
| Ambiente distinto a Test | 503 seguro |
| Marca de ambiente ausente o no legible | 503 seguro |

## Validacion

| Caso | Resultado |
| --- | --- |
| `cajaCodigo` vacio | 400 |
| `cajaCodigo` demasiado largo | 400 |
| `fondoInicial` nulo, cero o negativo | 400 |
| `fondoInicial` fuera de rango | 400 |
| `observacion` demasiado larga | 400 |
| Usuario JWT inexistente o inactivo | 400 |

## Negocio y concurrencia

| Caso | Resultado |
| --- | --- |
| Turno `Abierto` en la misma caja | 409 |
| Turno `EnCierre` en la misma caja | 409 |
| Apertura concurrente ganada por otra solicitud | 409 |
| Violacion del indice unico de turno abierto | 409 |
| Error despues de crear turno y antes del movimiento | rollback, 503 seguro |
| Error inesperado de transaccion | rollback, 503 seguro |

Las respuestas no deben exponer SQL, servidor, base, usuario, connection string, stack trace ni detalles internos.
