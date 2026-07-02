# Relacion venta, pago, caja e inventario para reversas

## Venta

La tabla de venta conserva la evidencia comercial original. En V1 no debe editarse el total, cliente, fecha, metodo de pago ni factura original.

## Detalle de venta

El detalle conserva los productos y cantidades originales. La recuperacion de inventario se registra como efecto compensatorio de la reversa, no como edicion historica del detalle.

## Pago

La tabla de pagos conserva el pago original. Para reversa V1 solo se aceptan ventas con pago efectivo total.

## Caja

El movimiento original `VentaEfectivo` queda intacto. La reversa futura debe generar un movimiento compensatorio auditable y enlazado al movimiento original cuando exista soporte persistente suficiente.

## Inventario

La reversa futura debe sumar de vuelta las cantidades vendidas y reducir vendido de forma controlada, dentro de la misma transaccion que caja y auditoria.

## Auditoria

La reversa debe registrar quien, cuando, motivo, estado, resultado de idempotencia y referencia de la venta original, sin modificar la evidencia original.

## Estado actual

La base ya contempla movimientos de caja tipo `Reversa`, pero no existe todavia un esquema especifico para reversas de venta integradas con inventario, pagos e idempotencia de venta.
