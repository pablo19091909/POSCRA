# Fase 4F.28 - Prueba ingreso WPF Caja API Test

Fecha UTC: 2026-06-30 14:13:07 UTC

## Resultado

Se ejecuto una prueba manual controlada desde `IngresoCajaPage` WPF hacia Caja API para registrar un unico ingreso sintetico por `100.00` en `Environment=Test`.

Flujo validado:

`IngresoCajaPage WPF -> CajaOperationCoordinator -> CajaApiClient -> POST /api/caja/ingresos -> POS.Api`

La prueba fue confirmada por el operador desde la interfaz real WPF. El formulario mostro modo Caja API, turno abierto y efectivo esperado desde API. El ingreso fue registrado correctamente por Caja API, el formulario se limpio despues del exito y el efectivo esperado aumento exactamente en `100.00`.

## Flags

Antes:

| Flag | Valor |
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

Durante:

| Flag | Valor temporal |
| --- | --- |
| `UseCajaApiIngresoWrite` | `true` solo local WPF |
| `EnableCajaApiWrite` | `true` solo proceso temporal POS.Api |

Despues:

| Flag | Valor |
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

La configuracion local y la copia efectiva de ejecucion WPF quedaron restauradas.

## Linea base

| Metrica | Antes | Despues |
| --- | ---: | ---: |
| `caja_turno` | 4 | 4 |
| Turnos `Abierto` Test | 1 | 1 |
| Turnos `EnCierre` Test | 0 | 0 |
| `movimiento_caja` | 10 | 11 |
| `caja_idempotencia` | 8 | 9 |
| Idempotencias `EnProceso` | 0 | 0 |
| `ingreso_caja` | 9 | 9 |
| `retiro_caja` | 6 | 6 |
| `cierre_caja` | 15 | 15 |
| `ventas` | 1948 | 1948 |
| `venta_pago` | 10 | 10 |
| `venta_idempotencia` | 10 | 10 |
| Inventario agregado | 3296.00 | 3296.00 |
| Saldo cliente agregado | -2957962.50 | -2957962.50 |
| Efectivo esperado | 1000.00 | 1100.00 |

## Movimiento e idempotencia

Se confirmo exactamente:

- un movimiento nuevo `IngresoCaja` por `100.00`;
- una idempotencia nueva de operacion `IngresoCaja` en estado `Completada`;
- cero idempotencias `EnProceso`;
- cero escrituras en `ingreso_caja`;
- cero cambios en tablas historicas y otros modulos monitoreados.

No se documentan identificadores internos, key, hash, usuario ni datos sensibles.

## Health y compilacion

Durante la activacion temporal:

| Endpoint | Resultado |
| --- | --- |
| `/health` | 200 |
| `/health/database` | 200 |
| `/api/system/version` | 200 |

Compilacion final:

| Proyecto | Resultado |
| --- | --- |
| WPF | Correcta, 0 errores |
| POS.Api | Correcta, 0 errores |

## Restauracion

POS.Api fue detenido al finalizar. El puerto local configurado para la API quedo sin escucha activa.

No se hizo rollback. El turno abierto y el ingreso sintetico se conservan como evidencia Test para la siguiente fase.

## Riesgos pendientes

- Retiro WPF sigue apagado.
- Cierre WPF sigue apagado.
- No existen reversas operativas.
- Ventas API efectivo aun no esta integrada con Caja API.
- No se validaron flujos de dolares, donacion ni pagos combinados contra Caja API.

## Recomendacion

Continuar con Fase 4F.29: validacion posterior ampliada de lectura WPF/API despues del ingreso sintetico, manteniendo escrituras apagadas y verificando que `IngresoCajaPage`, `RetirosCajaPage` y `CierreCajaPage` reflejen el nuevo pre-cierre sin SQL dual.
