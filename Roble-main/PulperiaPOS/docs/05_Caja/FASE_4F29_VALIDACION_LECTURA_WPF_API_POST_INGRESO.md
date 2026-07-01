# Fase 4F.29 - Validacion lectura WPF/API post ingreso

Fecha UTC: 2026-06-30.

## Resultado

Se valido la lectura WPF/API posterior al ingreso sintetico de Caja API, sin crear turnos, ingresos, retiros, cierres, movimientos, idempotencias, ventas ni cambios historicos.

## Flags

| Flag | Estado |
| --- | --- |
| `UseCajaApiRead` | `true` |
| `UseCajaApiOpenWrite` | `false` |
| `UseCajaApiIngresoWrite` | `false` |
| `UseCajaApiRetiroWrite` | `false` |
| `UseCajaApiCierreWrite` | `false` |
| `UseVentasApiWrite` | `false` |
| `EnableCajaApiWrite` | `false` |
| `EnableVentasApiWrite` | `false` |
| `EnableLegacyHashUpgrade` | `false` |

## Linea base e integridad

| Metrica | Antes | Despues |
| --- | ---: | ---: |
| `caja_turno` | 4 | 4 |
| Turnos `Abierto` Test | 1 | 1 |
| Turnos `EnCierre` Test | 0 | 0 |
| `movimiento_caja` | 11 | 11 |
| `caja_idempotencia` | 9 | 9 |
| Idempotencias `EnProceso` | 0 | 0 |
| `ingreso_caja` | 9 | 9 |
| `retiro_caja` | 6 | 6 |
| `cierre_caja` | 15 | 15 |
| `ventas` | 1948 | 1948 |
| `venta_pago` | 10 | 10 |
| `venta_idempotencia` | 10 | 10 |
| Inventario agregado | 3296.00 | 3296.00 |
| Saldo cliente agregado | -2957962.50 | -2957962.50 |
| Efectivo esperado | 1100.00 | 1100.00 |

No hubo cambios de datos durante esta fase.

## Rutas API validadas

Se usaron solo rutas de lectura:

- `GET /api/caja/turnos/abierto`
- `GET /api/caja/turnos/{id}/movimientos`
- `GET /api/caja/turnos/{id}/pre-cierre`

Resultados:

| Escenario | Resultado |
| --- | --- |
| Sin token | 401 |
| Token sin `Caja.Ver` | 403 |
| Token con `Caja.Ver` | 200 |
| Health | 200 |
| Health database | 200 |
| Version | 200 |

Los tokens fueron generados y usados en memoria. No se imprimieron ni documentaron.

## Validacion visual WPF

Validacion manual confirmada por operador:

- `IngresoCajaPage`, `RetirosCajaPage` y `CierreCajaPage` mostraron indicador Caja API.
- Se visualizo el turno abierto.
- Se visualizo el efectivo esperado actualizado en `1100.00`.
- Se detecto un detalle de UX en el resumen: el texto mostraba `cantidad/total`, visualmente confuso.
- Se corrigio solo la presentacion del resumen para mostrar `TipoMovimiento: total`.
- El operador confirmo la correccion visual.

No se habilitaron escrituras en WPF.

## API no disponible

Se detuvo POS.Api temporalmente y el operador valido que WPF mostro mensaje seguro de servicio no disponible. No se observo fallback SQL ni presentacion de historicos como estado Caja API.

Luego se reinicio POS.Api temporalmente con escrituras apagadas y las lecturas volvieron a responder correctamente.

## Correccion minima aplicada

Archivo:

- `PulperiaPOS/CajaApiReadStatusViewHelper.cs`

Cambios:

- el log seguro ya no registra URL base de la API; solo registra si esta configurada;
- el resumen visual ya no muestra `cantidad/total`, sino `TipoMovimiento: total`.

No se modificaron calculos, rutas API, SQL, persistencia ni comportamiento financiero.

## Compilacion

| Proyecto | Resultado |
| --- | --- |
| WPF | Correcta, 0 errores; advertencias heredadas en build aislado |
| WPF salida normal | Correcta, 0 errores |
| POS.Api | Correcta, 0 errores |

## Restauracion

POS.Api temporal fue detenido. La instancia WPF de validacion fue cerrada. El puerto local configurado para API quedo sin escucha activa.

## Recomendacion

Continuar con Fase 4F.30: auditoria y preparacion no ejecutada de `RetirosCajaPage` para escritura por Caja API detras de `UseCajaApiRetiroWrite`, manteniendo `EnableCajaApiWrite=false` y sin registrar retiros reales todavia.
