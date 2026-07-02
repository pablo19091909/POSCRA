# Validacion inventario, caja y venta reversada

## Validado sin escritura

La linea base antes y despues de migracion e integracion bloqueada permanecio igual:

- ventas: 1950;
- detalle: 5089;
- pagos: 12;
- movimientos: 19;
- inventario agregado: 3293.00;
- saldo agregado clientes: -2957962.50;
- ingreso historico: sin cambios;
- retiro historico: sin cambios;
- cierre historico: sin cambios.

## Preparado para escritura futura

La transaccion de reversa futura:

- conserva venta original;
- conserva pago original;
- conserva `VentaEfectivo` original;
- restaura inventario;
- crea movimiento compensatorio `Reversa`;
- crea `venta_reversa`;
- crea auditoria `VentaReversada`;
- completa idempotencia.

## No ejecutado

No se valido impacto neto real porque la reversa debe originarse manualmente desde WPF.
