# Concurrencia ventas, caja y cierre de turno

## Orden de locks propuesto

Para venta efectiva integrada:

1. `venta_idempotencia WITH (UPDLOCK, HOLDLOCK)`.
2. `caja_turno WITH (UPDLOCK, HOLDLOCK)` por caja abierta.
3. `usuario`.
4. `cliente WITH (UPDLOCK, HOLDLOCK)`.
5. `inventario WITH (UPDLOCK, HOLDLOCK)`.
6. Escrituras de venta, pago, movimiento, auditoria e idempotencia.

## Compatibilidad con Caja API

Cierre de turno bloquea `caja_turno` y luego calcula movimientos con locks. Venta efectiva preparada bloquea primero el turno antes de tocar inventario y antes de insertar `VentaEfectivo`, reduciendo la ventana de carrera contra cierre.

## Comportamientos esperados

- Turno `Abierto`: permite venta efectiva si todos los flags estan activos.
- Turno `EnCierre`: rechaza porque no hay turno abierto elegible.
- Turno `Cerrado`: rechaza.
- Sin turno abierto: rechaza antes de venta/pago/inventario.
- Otra venta efectiva: serializa por turno e inventario.
- Ingreso/retiro: compite por turno y movimientos bajo las reglas de Caja API.

## Riesgo pendiente

El orden de locks debe validarse bajo concurrencia real en el Prompt Maestro 2 antes de crear la primera venta Test efectiva.
