# Decisiones, migracion o bloqueos VentaEfectivo

## Decisiones tomadas

- Usar `venta_idempotencia` como idempotencia principal.
- No crear `caja_idempotencia` para venta efectiva.
- Insertar `VentaEfectivo` dentro de la transaccion de venta.
- Relacionar `VentaEfectivo` con `factura` y `pago_id`.
- Usar `pago_id` como clave unica efectiva mediante indice existente.
- Mantener WPF sin conectar a nueva ruta en esta fase.

## Bloqueos no criticos

El esquema actual permite relacion segura por `pago_id`, pero el contrato API de venta no contiene caja logica. Para multiples cajas o produccion, debe definirse uno de estos enfoques:

- agregar `cajaCodigo` al contrato de venta;
- resolver caja por terminal/sesion autenticada;
- mapear usuario/punto de venta a caja en servidor.

## Migracion pendiente posible

No se requiere migracion inmediata para la implementacion bloqueada. Antes de escenarios complejos puede evaluarse:

- indice unico filtrado adicional por `factura` para `VentaEfectivo` confirmado si se mantiene una venta = un pago efectivo;
- tabla/mapeo de terminal a caja;
- soporte de reversas inmutables para anulaciones/devoluciones.

## Limitaciones fuera de alcance

- Dolares.
- Donacion.
- Pagos combinados.
- Produccion.
- Reversas.
- Cliente General y saldos historicos negativos.
