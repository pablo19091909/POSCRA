# Checklist restauracion seguridad cierre exacto WPF

Fecha UTC: 2026-07-01 01:57:24 UTC

## Flags WPF finales

| Flag | Estado |
| --- | --- |
| `UseCajaApiRead` | `true` |
| `UseCajaApiOpenWrite` | `false` |
| `UseCajaApiIngresoWrite` | `false` |
| `UseCajaApiRetiroWrite` | `false` |
| `UseCajaApiCierreWrite` | `false` |
| `UseVentasApiWrite` | `false` |

## Flags API finales

| Flag | Estado |
| --- | --- |
| `EnableCajaApiWrite` | `false` en configuracion |
| `EnableVentasApiWrite` | `false` |
| `EnableLegacyHashUpgrade` | `false` |

## API

| Validacion | Resultado |
| --- | --- |
| POS.Api detenido | Si |
| Puerto local de prueba libre | Si |
| Health checks antes de detener | 200 |

## Base de datos

| Validacion | Resultado |
| --- | --- |
| Turno Test cerrado | Si |
| Turno Test abierto restante | 0 |
| Turno `EnCierre` restante | 0 |
| Idempotencia `CerrarTurno Completada` del turno | 1 |
| Idempotencia `EnProceso` del turno | 0 |
| Idempotencia `Fallida` del turno | 0 |
| `CierreDiferencia` creado | 0 |
| Tablas historicas sin cambios | Si |
| Ventas/inventario/clientes sin cambios | Si |

## Seguridad

- No se dejaron flags de escritura activos.
- No se dejo POS.Api ejecutandose.
- No se reabrio ni elimino el turno cerrado.
- No se hizo rollback.
- No se borraron movimientos ni idempotencias.
- No se documentaron secretos, tokens, keys, `rowVersion`, connection strings, hosts, puertos, IDs internos ni datos personales.

## Recomendacion

La siguiente fase debe partir de un estado sin turno abierto para `CAJA_PRINCIPAL_TEST`. Antes de nuevas pruebas destructivas, preparar explicitamente un nuevo turno Test o definir una validacion no destructiva de lectura posterior al cierre.
