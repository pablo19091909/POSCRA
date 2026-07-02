# Validacion inventario y caja post reversa

Fecha UTC: 2026-07-02T01:46:29Z

## Producto de prueba

- Producto: `API_TEST_PROD_STOCK_ALTO`.
- Precio: `10.00`.
- Cantidad vendida: 1.

## Inventario

Secuencia validada:

- Stock antes de venta: 90.
- Stock despues de venta: 89.
- Stock despues de reversa: 90.

Resultado:

- La venta desconto exactamente 1 unidad.
- La reversa restauro exactamente 1 unidad.
- El efecto neto del producto de prueba fue 0.
- No se validaron cambios sobre productos ajenos como parte de una operacion de escritura.

## Caja

Ultimo turno Test:

- Fondo inicial: `1000.00`.
- Venta efectiva: `10.00`.
- Reversa compensatoria: `10.00`.
- Efectivo esperado final: `1000.00`.

Movimientos permitidos en el turno:

- `FondoInicial`: 1.
- `VentaEfectivo`: 1.
- `Reversa`: 1.

No se genero:

- `IngresoCaja`.
- `RetiroCaja`.
- Nuevo `CierreDiferencia`.
- Cambio en `cierre_caja`.

## Cierre

- Efectivo contado: `1000.00`.
- Diferencia: `0.00`.
- Estado final: `Cerrado`.

Resultado aprobado.

