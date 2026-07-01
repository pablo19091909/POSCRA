# Fase 4F.36 - Prueba cierre exacto WPF Caja API Test

Fecha UTC: 2026-07-01 01:57:24 UTC

## Autorizacion y alcance

Se ejecuto exclusivamente en `Environment=Test` la prueba controlada de cierre exacto real desde `CierreCajaPage` WPF hacia Caja API.

La autorizacion operacional permitia cerrar una unica vez el turno Test abierto de `CAJA_PRINCIPAL_TEST`, crear una sola idempotencia `CerrarTurno` en estado `Completada`, actualizar el turno a `Cerrado` y registrar los datos de cierre en `caja_turno`.

No se autorizo crear movimientos `CierreDiferencia`, turnos nuevos, ventas, pagos, cambios de inventario, saldos, usuarios, roles, permisos, migraciones, scripts SQL manuales ni registros historicos en `cierre_caja`, `ingreso_caja` o `retiro_caja`.

## Linea base

Metodo de lectura: health checks API, validacion visual WPF y consultas agregadas `SELECT` de solo lectura. No se imprimieron secretos, tokens, IDs internos, host, connection strings ni valores tecnicos sensibles.

Flags iniciales:

| Flag | Estado |
| --- | --- |
| `UseCajaApiRead` | `true` en configuracion local WPF |
| `UseCajaApiOpenWrite` | `false` |
| `UseCajaApiIngresoWrite` | `false` |
| `UseCajaApiRetiroWrite` | `false` |
| `UseCajaApiCierreWrite` | `false` |
| `EnableCajaApiWrite` | `false` en configuracion; activado solo por proceso temporal |
| `EnableVentasApiWrite` | `false` |
| `UseVentasApiWrite` | `false` |
| `EnableLegacyHashUpgrade` | `false` |

Linea base agregada antes del cierre:

| Metrica | Antes |
| --- | ---: |
| `environment_test_allowed` | 1 |
| Turno abierto `CAJA_PRINCIPAL_TEST` | 1 |
| Turno `EnCierre` | 0 |
| Fondo inicial | 1,000.00 |
| Ingresos Caja API del turno | 100.00 |
| Retiros Caja API del turno | 100.00 |
| Efectivo esperado | 1,000.00 |
| Movimiento `FondoInicial` del turno | 1 |
| Movimiento `IngresoCaja` API de 100.00 | 1 |
| Movimiento `RetiroCaja` API de 100.00 | 1 |
| `CierreDiferencia` del turno | 0 |
| Ajustes | 0 |
| Reversas | 0 |
| Idempotencia `CerrarTurno Completada` del turno | 0 |
| Idempotencia `EnProceso` del turno | 0 |
| Idempotencia `Fallida` del turno | 0 |

Lineas base globales:

| Tabla/agregado | Antes |
| --- | ---: |
| `caja_turno` | 4 |
| `movimiento_caja` | 12 |
| `caja_idempotencia` | 10 |
| `ingreso_caja` | 9 |
| `retiro_caja` | 6 |
| `cierre_caja` | 15 |
| `ventas` | 1948 |
| `venta_pago` | 10 |
| `venta_idempotencia` | 10 |
| Inventario agregado | 3,296.00 |
| Saldo agregado clientes | -2,957,962.50 |
| Clientes | 167 |

## Activacion temporal

Se activo temporalmente `UseCajaApiCierreWrite=true` solo en configuracion local WPF de desarrollo y salida local. Se mantuvieron apagados `UseCajaApiOpenWrite`, `UseCajaApiIngresoWrite`, `UseCajaApiRetiroWrite`, `UseVentasApiWrite` y `EnableVentasApiWrite`.

Se inicio POS.Api en `Development` con `EnableCajaApiWrite=true` solo como variable de entorno del proceso temporal. No se modifico la configuracion versionada de la API para habilitar escritura.

Health checks durante la prueba:

| Endpoint | Resultado |
| --- | --- |
| `/health` | 200 |
| `/health/database` | 200 |
| `/api/system/version` | 200 |

## Validacion visual y cierre

El operador confirmo la ejecucion desde WPF real y reporto cierre exitoso.

Flujo validado:

- `CierreCajaPage` mostro modo Caja API.
- El pre-cierre visual correspondio al turno Test abierto.
- El cierre se ejecuto con efectivo contado `1000.00`.
- La observacion usada fue la definida para la fase.
- La confirmacion mostro operacion Caja API irreversible, esperado, contado y diferencia estimada.
- El operador ejecuto una unica aceptacion final.
- No se repitio manualmente el cierre.

Resultado financiero posterior:

| Metrica | Despues |
| --- | ---: |
| Turno abierto `CAJA_PRINCIPAL_TEST` | 0 |
| Turno `EnCierre` | 0 |
| Turnos cerrados `CAJA_PRINCIPAL_TEST` | 4 |
| Efectivo esperado de cierre | 1,000.00 |
| Efectivo contado | 1,000.00 |
| Diferencia | 0.00 |
| Fecha UTC de cierre generada | Si |
| Usuario de cierre valido | Si |
| Observacion guardada | Si |
| `rowVersion` cambio | Si |

## Integridad posterior

| Metrica | Despues |
| --- | ---: |
| Movimiento `FondoInicial` del turno cerrado | 1 |
| Movimiento `IngresoCaja` API de 100.00 | 1 |
| Movimiento `RetiroCaja` API de 100.00 | 1 |
| `CierreDiferencia` | 0 |
| Ajustes | 0 |
| Reversas | 0 |
| Idempotencia `CerrarTurno Completada` del turno | 1 |
| Idempotencia `CerrarTurno EnProceso` del turno | 0 |
| Idempotencia `CerrarTurno Fallida` del turno | 0 |
| `caja_turno` | 4 |
| `movimiento_caja` | 12 |
| `caja_idempotencia` | 11 |
| `ingreso_caja` | 9 |
| `retiro_caja` | 6 |
| `cierre_caja` | 15 |
| `ventas` | 1948 |
| `venta_pago` | 10 |
| `venta_idempotencia` | 10 |
| Inventario agregado | 3,296.00 |
| Saldo agregado clientes | -2,957,962.50 |
| Clientes | 167 |

## Compilacion

| Proyecto | Resultado |
| --- | --- |
| WPF | Correcta, 0 errores, 284 advertencias heredadas |
| POS.Api | Correcta, 0 errores, 0 advertencias |

## Restauracion

Flags finales:

| Flag | Estado final |
| --- | --- |
| `UseCajaApiRead` | `true` en configuracion local WPF |
| `UseCajaApiOpenWrite` | `false` |
| `UseCajaApiIngresoWrite` | `false` |
| `UseCajaApiRetiroWrite` | `false` |
| `UseCajaApiCierreWrite` | `false` |
| `EnableCajaApiWrite` | `false` en configuracion |
| `EnableVentasApiWrite` | `false` |
| `UseVentasApiWrite` | `false` |
| `EnableLegacyHashUpgrade` | `false` |

POS.Api fue detenido y el puerto local usado por la prueba quedo libre.

## Confirmaciones

- No se uso Postman ni script HTTP para cerrar el turno.
- El cierre se origino desde WPF real.
- No se creo `CierreDiferencia`.
- No se creo movimiento adicional.
- No se escribio en `cierre_caja`, `ingreso_caja`, `retiro_caja`, `ventas`, `venta_pago`, `venta_idempotencia`, `inventario` ni `cliente`.
- No hubo fallback SQL historico, dual write ni impresion historica.
- No se revelaron JWT, keys, hashes, `rowVersion`, connection strings, IDs internos ni datos personales.

## Riesgos pendientes

- No se ha validado cierre con sobrante o faltante desde WPF.
- No existen reversas inmutables.
- Ventas API en efectivo aun no estan integradas con Caja API.
- Dolares, Donacion y pagos combinados permanecen deshabilitados.

## Recomendacion

Avanzar a Fase 4F.37 para preparar un nuevo turno Test de caja o definir la siguiente validacion controlada posterior al cierre exacto, manteniendo escrituras apagadas por defecto y sin reabrir ni alterar el turno cerrado.
