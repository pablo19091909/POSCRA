# Checklist restauracion seguridad ingreso WPF

Fecha UTC: 2026-06-30 14:13:07 UTC

## Restauracion de flags

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

La copia local de desarrollo y la copia efectiva de ejecucion WPF fueron restauradas.

## Procesos

| Elemento | Resultado |
| --- | --- |
| POS.Api temporal | Detenido |
| Puerto local configurado para API | Sin escucha activa |
| Health durante activacion | 200 en endpoints requeridos |

## Base de datos

| Validacion | Resultado |
| --- | --- |
| `Environment=Test` | Confirmado |
| Escrituras permitidas para pruebas | Confirmado |
| Turno Test abierto | 1 |
| Turno Test en cierre | 0 |
| Idempotencias en proceso | 0 |
| Rollback | No ejecutado |
| Eliminacion de evidencia | No ejecutada |

## Evidencia conservada

Se conserva el turno abierto y el ingreso sintetico creado como evidencia Test para la siguiente fase.

## Pendientes

- Retiro WPF por API permanece apagado.
- Cierre WPF por API permanece apagado.
- Reversas permanecen fuera de alcance.
- Integracion ventas efectivo con Caja API permanece pendiente.
- Dolares, donacion y pagos combinados permanecen fuera de alcance.
