# Matriz de errores venta API

| Caso | HTTP previsto | Respuesta segura |
| --- | ---: | --- |
| Feature flag apagado | 503 | Venta API no disponible |
| Sin token | 401 | Respuesta estandar de autenticacion |
| Sin permiso `Ventas.Crear` | 403 | Respuesta estandar de autorizacion |
| Request invalido | 400 | Solicitud de venta invalida |
| Cliente inexistente | 400 | Cliente no encontrado |
| Producto inexistente | 400 | Producto no encontrado |
| Producto sin precio valido | 400 | Producto no disponible para venta |
| Stock insuficiente | 400 | Stock insuficiente |
| Saldo insuficiente | 400 | Saldo insuficiente |
| Monto recibido insuficiente | 400 | Monto recibido insuficiente |
| Metodo no soportado | 400 | Metodo de pago no soportado |
| Donacion | 400 | Donacion no soportada |
| Dolares no habilitado | 400 | Pago en dolares no habilitado |
| Misma idempotency key con request distinto | 409 | Conflicto de idempotencia |
| Solicitud en proceso | 409 | Solicitud de venta en proceso |
| Estado de idempotencia no recuperable | 409 | Estado de idempotencia no permite continuar |
| Error transaccional inesperado | 400 actual / endurecer despues | No fue posible completar la venta |

## Nota

En Fase 4C.2 la escritura sigue bloqueada por feature flag. Los errores transaccionales se preparan para la fase de pruebas con flag activo.

No se devuelven SQL, stack traces, connection strings, tokens, hashes ni datos internos.
