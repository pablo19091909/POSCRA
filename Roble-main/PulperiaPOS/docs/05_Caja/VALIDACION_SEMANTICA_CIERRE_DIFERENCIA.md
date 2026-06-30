# Validacion semantica de CierreDiferencia

Fecha/hora UTC: 2026-06-29 23:34:01 UTC

## Regla validada

`CierreDiferencia` representa la auditoria del cierre y no debe alterar el efectivo esperado previo del turno.

La diferencia con signo vive en `caja_turno.diferencia`.

El movimiento `CierreDiferencia` usa monto positivo absoluto.

## Evidencia agregada

Turno A, sobrante:

- efectivo esperado recalculado sin `CierreDiferencia`: 1000.00;
- diferencia del turno: +5.00;
- movimiento `CierreDiferencia`: 1 por 5.00.

Turno B, faltante:

- efectivo esperado recalculado sin `CierreDiferencia`: 1000.00;
- diferencia del turno: -5.00;
- movimiento `CierreDiferencia`: 1 por 5.00.

Validaciones globales:

- movimientos `CierreDiferencia` negativos: 0;
- cierres con diferencia sin movimiento de auditoria: 0;
- cierres exactos con movimiento de diferencia: 0;
- movimientos no autorizados en los turnos de fase: 0;
- reversas en los turnos de fase: 0;
- movimientos huerfanos en los turnos de fase: 0.

## Aislamiento historico

Los siguientes valores permanecieron iguales respecto a la linea base:

- `ingreso_caja`: 9;
- `retiro_caja`: 6;
- `cierre_caja`: 15;
- `ventas`: 1948;
- `venta_pago`: 10;
- `venta_idempotencia`: 10;
- inventario agregado: 3296.00;
- saldo agregado de clientes: -2957962.50.

## Resultado

La semantica de diferencia quedo validada para sobrante y faltante.

