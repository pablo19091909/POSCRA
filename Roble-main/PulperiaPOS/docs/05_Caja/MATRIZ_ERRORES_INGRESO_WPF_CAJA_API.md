# Matriz errores - Ingreso WPF Caja API

Fecha/hora UTC: 2026-06-30T13:45:44Z

| Error | Mensaje WPF seguro | Fallback SQL |
| --- | --- | --- |
| 400 / bad request | Solicitud invalida. Revise los datos ingresados. | No |
| 401 / sesion vencida | Su sesion ha vencido. Inicie sesion nuevamente. | No |
| 403 / sin permiso | No tiene permiso para registrar ingresos de caja. | No |
| 404 / recurso no encontrado | No hay un turno abierto para registrar el ingreso. | No |
| 409 / conflicto | No hay un turno abierto para registrar el ingreso. | No |
| 503 / servicio no disponible | No fue posible comunicarse con el servicio de caja. Intente nuevamente. | No |
| Timeout | No fue posible confirmar el resultado de la operacion. Revise la conexion y reintente sin cambiar los datos. | No |
| Red caida | No fue posible comunicarse con el servicio de caja. Intente nuevamente. | No |
| Respuesta invalida | La respuesta del servicio no pudo ser interpretada. | No |

No se muestran tokens, keys, SQL, endpoint, connection strings, hashes, stack traces ni detalles internos.
