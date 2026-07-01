# Validacion no dual write cierre WPF

Fecha UTC: 2026-07-01 01:57:24 UTC

## Objetivo

Confirmar que el cierre exacto de la Fase 4F.36 fue persistido solo por Caja API y no por el flujo historico SQL de `CierreCajaPage`.

## Evidencia

Durante la prueba se mantuvo `UseCajaApiCierreWrite=true` temporalmente solo para WPF local y `EnableCajaApiWrite=true` solo en el proceso temporal de POS.Api.

El resultado esperado autorizado era:

- actualizar `caja_turno`;
- crear una idempotencia `CerrarTurno Completada`;
- no crear `CierreDiferencia`;
- no escribir tablas historicas.

## Conteos antes y despues

| Tabla/agregado | Antes | Despues | Resultado |
| --- | ---: | ---: | --- |
| `caja_turno` | 4 | 4 | Sin nuevo turno |
| `movimiento_caja` | 12 | 12 | Sin nuevo movimiento |
| `caja_idempotencia` | 10 | 11 | +1 idempotencia esperada |
| `ingreso_caja` | 9 | 9 | Sin cambios |
| `retiro_caja` | 6 | 6 | Sin cambios |
| `cierre_caja` | 15 | 15 | Sin cambios |
| `ventas` | 1948 | 1948 | Sin cambios |
| `venta_pago` | 10 | 10 | Sin cambios |
| `venta_idempotencia` | 10 | 10 | Sin cambios |
| Inventario agregado | 3,296.00 | 3,296.00 | Sin cambios |
| Saldo agregado clientes | -2,957,962.50 | -2,957,962.50 | Sin cambios |
| Clientes | 167 | 167 | Sin cambios |

## Confirmaciones

- No se llamo el flujo historico para persistir cierre.
- No se escribio en `cierre_caja`.
- No se ejecuto dual write.
- No se imprimio comprobante historico.
- No se uso `RawPrinterHelper` para el cierre API.
- No se usaron scripts SQL manuales para cerrar el turno.
- El flujo historico permanece disponible solo cuando el flag API esta apagado.

## Resultado

Aprobado. El cierre exacto WPF se persistio por Caja API sin dual write y sin escritura historica.
