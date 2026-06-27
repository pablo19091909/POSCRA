# Limitaciones V1 ventas API

## No soportado en V1

- Escritura productiva de ventas API.
- Conexion WPF a `POST /api/ventas`.
- Pagos combinados.
- Donaciones mediante `POST /api/ventas`.
- Anulaciones.
- Devoluciones.
- Edicion o borrado de ventas.
- Backfill historico de pagos, auditorias o idempotencias.
- CajaTurno.
- MovimientoCaja.
- Reglas finales de cierre de caja.

## Metodos de pago V1

Permitidos inicialmente:

- `Efectivo`
- `Tarjeta`
- `Sinpe`
- `Dolares`
- `SaldoCliente`

Excluido:

- `Donación`

## Motivo de exclusion de donaciones

`Donación` existe como metodo historico, pero su flujo debe definirse de forma separada para evitar mezclar venta comercial, donacion y caja sin reglas contables claras.

## Motivo de excluir pagos combinados

El flujo actual no tiene pagos combinados reales. La primera version debe cerrar una venta con un solo metodo antes de ampliar complejidad.

## Riesgo pendiente

No activar `EnableVentasApiWrite=true` hasta que exista transaccion completa con validacion de cliente, productos, precio, stock, saldo, pago, auditoria e idempotencia.
