# Resultados idempotencia, concurrencia y rollback Test

Fecha/hora UTC: 2026-06-26

## Idempotencia

### Caso M

Misma `IdempotencyKey` y mismo request:

- Primera llamada: HTTP 200.
- Segunda llamada: HTTP 200.
- Resultado: repeticion segura.
- No se genero segunda venta.

### Caso N

Misma `IdempotencyKey` con request distinto:

- Resultado: HTTP 409.
- No se creo venta adicional.
- No cambio stock ni saldo por la segunda solicitud.

## Concurrencia

### Caso O

Dos solicitudes casi simultaneas con claves distintas sobre `API_TEST_PROD_STOCK_UNIDAD`:

- Resultado: HTTP 200 y HTTP 400.
- Como maximo una venta consumio la ultima unidad.
- Stock final no negativo.
- Sin ventas parciales.

## Rollback

Los casos F-L validaron rollback/control de fallos:

- Stock insuficiente.
- Saldo insuficiente.
- Monto efectivo insuficiente.
- Producto inexistente.
- Cliente inexistente.
- Donacion no soportada.
- Dolares no habilitado.

Resultado final:

- Pagos huerfanos: 0.
- Auditorias huerfanas: 0.
- Detalles huerfanos: 0.
- Stock negativo `API_TEST_`: 0.
- Saldo negativo `API_TEST_`: 0.

## Nota tecnica

No se dejo ningun bypass ni flag de debug permanente. La simulacion de rollback se hizo mediante errores funcionales controlados dentro de la transaccion.
