# Resultados reversa manual WPF con Caja API

Fecha UTC: 2026-07-02T01:46:29Z

## Resumen

La reversa manual WPF de una venta efectiva API fue ejecutada y validada correctamente en ambiente Test.

## Venta de prueba

- Factura: `2002`.
- Total: `10.00`.
- Metodo de pago: Efectivo.
- Producto: `API_TEST_PROD_STOCK_ALTO`.
- Cantidad: 1.

## Resultado de reversa

- La venta original permanece existente.
- Se creo exactamente una fila en `venta_reversa`.
- Se creo exactamente un movimiento compensatorio `Reversa`.
- El pago original permanece registrado.
- La reversa quedo asociada a la venta original.
- No hay reversas huerfanas.
- No hay idempotencias `EnProceso`.
- No hay idempotencias `Fallida`.

## Caja

Movimientos del ultimo turno Test:

- `FondoInicial`: 1 movimiento por `1000.00`.
- `VentaEfectivo`: 1 movimiento por `10.00`.
- `Reversa`: 1 movimiento por `10.00`.

Resultado:

- Efectivo esperado despues de reversa: `1000.00`.
- No se creo ingreso historico.
- No se creo retiro historico.
- No se creo movimiento nuevo de `CierreDiferencia`.

## Cierre

- Estado final: `Cerrado`.
- Efectivo esperado: `1000.00`.
- Efectivo contado: `1000.00`.
- Diferencia: `0.00`.

## Confirmaciones

- No hubo borrado fisico.
- No hubo fallback SQL para escritura de venta, reversa o cierre.
- No hubo dual write.
- No hubo impresion historica en la venta API.
- La API uso `https://localhost:7046`.

