# Politica pagos V1 venta API

## Alcance

V1 admite exactamente un pago por venta.

No admite pagos combinados.

## Efectivo

- Requiere `montoRecibido`.
- `montoRecibido` debe ser mayor o igual al total recalculado.
- Vuelto = `montoRecibido - total`.
- `venta_pago.monto` guarda el total.
- `venta_pago.monto_recibido` guarda el monto recibido.
- `venta_pago.vuelto` guarda el vuelto calculado por servidor.

Caja futura debe usar el efecto neto de la venta. No debe sumar monto recibido y restar vuelto de forma que duplique calculos.

## Tarjeta

- Requiere voucher.
- No aplica vuelto.
- `montoRecibido` debe ser nulo o igual al total.
- No modifica caja en esta fase.

## Sinpe

- Requiere referencia.
- No aplica vuelto.
- `montoRecibido` debe ser nulo o igual al total.
- No modifica caja en esta fase.

## SaldoCliente

- No admite monto recibido.
- No admite vuelto.
- Descuenta saldo con `UPDATE` condicionado dentro de la transaccion.
- No modifica caja.

## Dolares

Queda preparado pero no habilitado para escritura API V1.

Motivo: `TipoCambioDolar` usa columnas `real`; se requiere una fuente decimal server-side antes de activar conversiones monetarias.

## Donacion

No soportada por `POST /api/ventas` V1.

Debe permanecer en un flujo separado hasta definir reglas contables y de caja.
