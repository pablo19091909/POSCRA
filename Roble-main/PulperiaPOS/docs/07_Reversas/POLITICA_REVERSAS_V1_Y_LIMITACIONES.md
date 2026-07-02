# Politica de reversas V1 y limitaciones

## Permitido en V1 futura

- Reversa total de venta confirmada.
- Pago 100% efectivo.
- Venta perteneciente al turno actualmente abierto.
- Usuario con permiso explicito.
- Motivo obligatorio.
- Idempotency key obligatoria.
- Ejecucion solo en ambiente Test hasta nueva autorizacion.

## No permitido en V1

- Reversa parcial.
- Devolucion parcial.
- Pagos combinados.
- Tarjeta.
- SINPE.
- Dolares.
- Saldo cliente.
- Venta de turno cerrado.
- Venta de turno en cierre.
- Reversa de reversa.
- Borrado de venta.
- Edicion del pago original.
- Edicion del movimiento original de caja.
- Reapertura de cierres.

## Flags obligatorias

Para una ejecucion futura deben estar activas todas las compuertas:

- WPF: `UseVentasApiReversaWrite=true`.
- API: `EnableVentasApiWrite=true`.
- API: `EnableCajaApiWrite=true`.
- API: `EnableVentasApiReversaCajaWrite=true`.

## Estado actual

Todas las compuertas permanecen apagadas. El endpoint responde de forma segura y no modifica datos.
