# Matriz de pruebas para futura API de ventas

Fecha UTC: 2026-06-26 10:45:52 UTC

## Matriz funcional

| Caso | Entrada | Resultado esperado |
|---|---|---|
| Efectivo exacto | monto recibido igual al total | venta registrada, vuelto cero |
| Efectivo con vuelto | monto recibido mayor al total | venta registrada, vuelto calculado por API |
| Efectivo insuficiente | monto recibido menor al total | rechazo seguro |
| Tarjeta | voucher presente | venta registrada sin aumentar efectivo fisico |
| Tarjeta sin voucher | referencia vacia | rechazo seguro |
| SINPE | comprobante presente | venta registrada sin aumentar efectivo fisico |
| SINPE sin comprobante | referencia vacia | rechazo seguro |
| Dolares | monto USD suficiente | API aplica tipo de cambio vigente y calcula vuelto |
| Dolares sin tipo de cambio | no hay tipo vigente | rechazo seguro |
| Saldo cliente suficiente | saldo >= total | venta registrada y saldo descontado |
| Saldo insuficiente | saldo < total | rechazo seguro sin venta |
| Multiples items | varios productos | detalles creados y stock descontado por todos |
| Producto inexistente | productoId invalido | rechazo seguro |
| Producto sin stock | stock <= 0 | rechazo seguro |
| Cantidad invalida | cantidad <= 0 | rechazo seguro |

## Matriz transaccional

| Caso | Fallo inducido | Resultado esperado |
|---|---|---|
| Error insertando detalle | falla despues de encabezado | rollback: sin encabezado ni detalle |
| Error descontando stock | falla luego de detalle | rollback: sin venta ni stock alterado |
| Error descontando saldo | saldo condicionado falla | rollback total |
| Error registrando pago | falla despues de stock | rollback total |
| Error registrando caja | falla movimiento caja | rollback o estado pendiente definido |
| Ultimo producto simultaneo | dos solicitudes compiten | solo una confirma; otra recibe stock insuficiente |
| Doble clic | dos requests iguales | idempotencia devuelve misma venta o rechaza duplicado controlado |
| Reintento de red | mismo idempotencyKey | no duplica venta |

## Matriz seguridad

| Caso | Resultado esperado |
|---|---|
| Sin token | HTTP 401 |
| Token vencido | HTTP 401 y WPF limpia sesion |
| Usuario sin `Ventas.Crear` | HTTP 403 |
| Usuario inactivo | rechazo seguro |
| Payload manipula precio | API ignora precio enviado |
| Payload manipula total | API recalcula total |
| Producto no autorizado/inactivo futuro | rechazo seguro |

## Matriz caja futura

| Caso | Resultado esperado |
|---|---|
| Venta con caja abierta | movimiento de caja registrado |
| Venta con caja cerrada | rechazo o regla explicita |
| Venta despues de cierre | no cae en cierre anterior |
| Cierre durante venta | aislamiento transaccional consistente |
| Anulacion futura | movimiento reverso, sin borrado fisico |
| Devolucion futura | movimiento de stock/caja auditado |

## Validaciones de datos

- Totales API coinciden con suma de detalles recalculados.
- `ventas.total` no nullable en modelo futuro.
- `usuario_id` obligatorio.
- fecha/hora UTC generada por servidor.
- stock nunca negativo.
- saldo nunca negativo salvo politica formal de credito.
- cada venta tiene auditoria.
- cada solicitud tiene idempotency key.
