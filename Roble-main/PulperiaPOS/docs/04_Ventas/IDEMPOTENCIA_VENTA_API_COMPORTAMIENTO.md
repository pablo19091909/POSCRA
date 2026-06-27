# Idempotencia venta API - comportamiento

## Clave

La idempotencia se resuelve por:

`usuario_id + idempotency_key`

## Hash

Se calcula SHA-256 deterministico sobre la intencion de venta:

- Cliente.
- Items.
- Pago.
- Observaciones.
- Tipo de cambio observado.
- Referencia o voucher.

No incluye token, claims completos, secretos, factura ni fecha oficial.

## Nueva solicitud

Si no existe registro, se crea `EnProceso` dentro de la transaccion.

## Misma clave y mismo hash completada

No crea venta nueva. Recupera respuesta equivalente de la venta ya completada.

## Misma clave y hash distinto

Devuelve conflicto seguro. No crea venta.

## Misma clave en proceso

Devuelve conflicto de solicitud en proceso. No crea venta.

## Misma clave fallida

Si esta `Fallida` y no tiene factura, se permite reintento marcandola de nuevo como `EnProceso` dentro de la transaccion.

Si tiene factura o estado no recuperable, responde conflicto.

## Fallos durante transaccion

La transaccion se revierte. La estrategia de marcar `Fallida` persistente queda pendiente de prueba controlada, porque no se debe introducir un segundo write fuera de la transaccion sin validar casos de timeout o resultado desconocido.

## Estado con flag apagado

Con `EnableVentasApiWrite=false`, no se consulta ni se escribe idempotencia desde `POST /api/ventas`.
