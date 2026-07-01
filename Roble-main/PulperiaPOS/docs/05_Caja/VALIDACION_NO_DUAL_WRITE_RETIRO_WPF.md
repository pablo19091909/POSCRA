# Validacion no dual write - Retiro WPF

Fecha UTC: 2026-06-30

## Objetivo

Confirmar que el retiro sintetico autorizado se registro por Caja API sin escritura historica paralela.

## Evidencia agregada

Despues de la prueba:

- `movimiento_caja` tipo `RetiroCaja` aumento en 1.
- Total `RetiroCaja` global paso a 3 movimientos y 1400.00 acumulado.
- Retiros del turno abierto: 100.00.
- `retiro_caja` historico permanecio en 6.
- `ingreso_caja` permanecio en 9.
- `cierre_caja` permanecio en 15.
- `ventas` permanecio en 1948.
- `venta_pago` permanecio en 10.
- `venta_idempotencia` permanecio en 10.
- Inventario agregado permanecio en 3296.00.
- Saldo agregado de clientes permanecio en -2957962.50.

## Confirmacion

No hubo dual write:

- No se escribio en `retiro_caja`.
- No se escribio en ventas.
- No se escribio en pagos.
- No se modifico inventario.
- No se modificaron saldos de clientes.
- No se creo cierre.
- No se creo ingreso.

## Impresion

No se evidencio impresion historica durante el flujo API.

## Limitacion

No se inspeccionaron trazas internas con nombres tecnicos sensibles en la documentacion. La validacion se baso en comportamiento visual y agregados de base.
