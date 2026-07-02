# Migracion de persistencia para reversa inmutable

## Migracion real

`database/migrations/011_ReversaVentaEfectivoInmutable.sql`

## Tabla nueva

`dbo.venta_reversa`

Columnas principales:

- `idReversa`
- `factura`
- `idPago`
- `idMovimientoVentaEfectivo`
- `idMovimientoCompensatorio`
- `idIdempotencia`
- `usuario_id`
- `motivo`
- `monto`
- `moneda`
- `estado`
- `fecha_hora_utc`
- `trace_id`
- `observaciones`
- `creado_utc`

## Constraints e indices

- PK `PK_venta_reversa`.
- FKs hacia venta, pago, movimiento original, movimiento compensatorio, idempotencia y usuario.
- `UX_venta_reversa_factura_activa`.
- `UX_venta_reversa_pago_activa`.
- `UX_venta_reversa_movimiento_compensatorio`.
- `UX_venta_reversa_idempotencia`.
- `IX_venta_reversa_fecha`.
- checks de monto, moneda, estado y motivo.

## Auditoria

`CK_venta_auditoria_evento` ahora permite `VentaReversada`.

## Efecto en datos

No se insertaron ventas, pagos, movimientos, auditorias ni idempotencias. La tabla quedo preparada para pruebas futuras.
