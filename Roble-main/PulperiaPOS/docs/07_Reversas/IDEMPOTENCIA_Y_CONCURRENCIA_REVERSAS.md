# Idempotencia y concurrencia de reversas

## Objetivo

Evitar dobles reversas, duplicidad de movimientos de caja y recuperacion doble de inventario.

## Reglas propuestas

- Toda reversa debe exigir `IdempotencyKey`.
- La misma key con el mismo payload debe devolver el mismo resultado.
- La misma key con payload diferente debe devolver conflicto.
- Una venta ya reversada debe devolver conflicto.
- Dos solicitudes concurrentes para la misma venta deben permitir solo una reversa confirmada.
- Una solicitud en progreso debe devolver conflicto temporal.
- Los estados pendientes no deben quedar indefinidamente sin mecanismo de recuperacion operativa.

## Bloqueos recomendados

- Bloqueo transaccional de la venta original.
- Validacion transaccional del movimiento de caja original.
- Validacion transaccional de turno abierto.
- Restriccion unica sobre venta reversada.
- Restriccion unica sobre idempotency key.

## Estado actual

La API valida autorizacion y flags antes de cualquier intento de escritura. La ejecucion real sigue bloqueada hasta que exista una migracion de soporte.
