# Validacion inventario y Caja venta efectivo

Fecha UTC: 2026-07-01 03:27:11 UTC

## Producto

Se uso un producto sintetico de stock alto, activo para pruebas y con precio valido.

No se documenta su identificador interno.

## Inventario

Resultado de la venta unica:

- Cantidad vendida: 1.
- Variacion de stock del producto elegido: -1.00.
- No hubo segundo descuento durante el reintento idempotente.
- No hubo descuento durante el conflicto 409.
- No hubo descuento durante la solicitud invalida de rollback.

## Caja

Se abrio un turno Test nuevo con fondo inicial de 1000.00.

Posterior a la venta:

- Turnos abiertos de la caja Test: 1.
- Turnos en cierre de la caja Test: 0.
- `FondoInicial`: 1 movimiento.
- `VentaEfectivo`: 1 movimiento.
- `IngresoCaja`: 0 movimientos.
- `RetiroCaja`: 0 movimientos.
- Ajustes: 0.
- Reversas: 0.
- `CierreDiferencia`: 0.

## Pre-cierre

El endpoint de pre-cierre devolvio:

- Fondo inicial: 1000.00.
- Venta efectivo: 10.00.
- Efectivo esperado: 1010.00.

## Integridad historica

No hubo cambios en:

- `ingreso_caja`.
- `retiro_caja`.
- `cierre_caja`.

## Conclusion

La venta API en efectivo desconto inventario exactamente una vez y aumento el efectivo esperado del turno Test exactamente por el total de la venta.
