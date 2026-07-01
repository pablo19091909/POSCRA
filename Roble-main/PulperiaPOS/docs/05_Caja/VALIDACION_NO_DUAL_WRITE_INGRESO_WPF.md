# Validacion no dual write ingreso WPF

Fecha UTC: 2026-06-30 14:13:07 UTC

## Ruta autorizada

La operacion autorizada fue ejecutada desde WPF usando la ruta Caja API:

`IngresoCajaPage WPF -> CajaOperationCoordinator -> CajaApiClient -> POS.Api`

## Persistencia confirmada

Se confirmo por agregados:

| Tabla o modulo | Antes | Despues | Cambio |
| --- | ---: | ---: | ---: |
| `movimiento_caja` | 10 | 11 | +1 |
| `caja_idempotencia` | 8 | 9 | +1 |
| `ingreso_caja` | 9 | 9 | 0 |
| `retiro_caja` | 6 | 6 | 0 |
| `cierre_caja` | 15 | 15 | 0 |
| `ventas` | 1948 | 1948 | 0 |
| `venta_pago` | 10 | 10 | 0 |
| `venta_idempotencia` | 10 | 10 | 0 |
| Inventario agregado | 3296.00 | 3296.00 | 0 |
| Saldo cliente agregado | -2957962.50 | -2957962.50 | 0 |

## Confirmaciones

- No se escribio en `ingreso_caja`.
- No se genero escritura dual.
- No hubo fallback SQL para persistir el ingreso.
- No se uso `CajaHelper` para confirmar la operacion API.
- No se uso `RawPrinterHelper` en la ruta API.
- No se modificaron ventas, pagos, inventario, clientes, retiros ni cierres.

La pantalla puede seguir leyendo datos historicos existentes para su UI heredada, pero la persistencia del ingreso autorizado no uso la tabla historica.
