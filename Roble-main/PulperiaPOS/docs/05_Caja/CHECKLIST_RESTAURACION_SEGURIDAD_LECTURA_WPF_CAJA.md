# Checklist restauracion seguridad lectura WPF Caja

Fecha UTC: 2026-06-30.

## Flags finales

| Flag | Estado final |
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

## Procesos

| Elemento | Resultado |
| --- | --- |
| POS.Api temporal | Detenido |
| WPF de validacion | Cerrado |
| Puerto local API | Sin escucha activa |

## Base de datos

| Validacion | Resultado |
| --- | --- |
| `Environment=Test` | Confirmado |
| Escrituras permitidas solo para pruebas | Confirmado |
| Turno Test abierto | 1 |
| Turno Test en cierre | 0 |
| Movimientos | Sin cambios |
| Idempotencias | Sin cambios |
| Historicos | Sin cambios |
| Ventas/pagos/inventario/clientes | Sin cambios |

## Seguridad

- No se imprimieron tokens.
- No se imprimieron credenciales.
- No se imprimieron connection strings.
- No se documentaron identificadores internos.
- No se dejo POS.Api ejecutandose.
- No se activo ninguna escritura.

## Pendientes

- Preparar `RetirosCajaPage` por API detras de flag.
- Mantener `EnableCajaApiWrite=false` hasta prueba controlada autorizada.
- Definir UX final para pre-cierre completo en `CierreCajaPage`.
