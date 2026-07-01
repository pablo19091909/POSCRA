# Matriz errores retiro WPF Caja API

| Escenario | Codigo API | Mensaje WPF seguro | Accion WPF |
| --- | --- | --- | --- |
| Sin token | 401 | Su sesion ha vencido. Inicie sesion nuevamente. | No escribir, no fallback |
| Sin `Caja.Retirar` | 403 | No tiene permiso para registrar retiros de caja. | No escribir, no fallback |
| Escritura apagada | 503 | El registro de retiros por API no esta habilitado para este ambiente. | Restaurar UI |
| Sin turno abierto | 404/409 | No hay un turno abierto para registrar el retiro. | Restaurar UI |
| Turno no permite retiros | 409 | El monto solicitado supera el efectivo disponible de la caja o el turno no permite retiros. | Restaurar UI |
| Efectivo insuficiente | 409 | El monto solicitado supera el efectivo disponible de la caja o el turno no permite retiros. | Restaurar UI |
| Timeout | Timeout | No fue posible confirmar el resultado de la operacion. Revise la conexion y reintente sin cambiar los datos. | Conservar formulario y key |
| Red/API no disponible | Network/503 | No fue posible comunicarse con el servicio de caja. Intente nuevamente. | Permitir reintento |
| Respuesta invalida | InvalidResponse | Respuesta invalida del servicio. | No fallback |

Con `EnableCajaApiWrite=false`, la prueba tecnica confirmo `503` con permiso aun cuando se envie key valida.
