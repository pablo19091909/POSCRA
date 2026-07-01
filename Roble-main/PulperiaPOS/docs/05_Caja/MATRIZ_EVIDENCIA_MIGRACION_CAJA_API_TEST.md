# Matriz evidencia migracion Caja API Test

Fecha UTC: 2026-07-01 02:46:50 UTC

| Evidencia | Estado |
| --- | --- |
| Environment=Test | Validado |
| `writes_allowed_for_testing=1` | Validado |
| Apertura WPF/API | Validada |
| Ingreso WPF/API | Validado en fases previas |
| Retiro WPF/API | Validado en fases previas |
| Lectura/pre-cierre WPF/API | Validado |
| Ausencia de turno abierto | Validada |
| API caida / mensajes seguros | Validado en fases previas |
| Flags apagados por defecto | Validado |
| Cierre exacto WPF/API | Validado en Fase 4F.36 |
| Cierre con sobrante WPF/API | Validado |
| Cierre con faltante WPF/API | Validado |
| `CierreDiferencia` para sobrante | Validado |
| `CierreDiferencia` para faltante | Validado |
| Diferencia exacta sin `CierreDiferencia` | Validado |
| Idempotencia apertura | Validada |
| Idempotencia cierre | Validada |
| Doble clic / segunda intencion | Sin duplicados observados |
| No fallback SQL | Validado |
| No dual write | Validado |
| No impresion historica | Validado |
| Sin pendientes `EnProceso` | Validado |
| Sin fallidas de estas pruebas | Validado |

## Semantica financiera

| Caso | Diferencia | Movimiento |
| --- | ---: | ---: |
| Exacto | 0.00 | 0 `CierreDiferencia` |
| Sobrante | 5.00 | 1 movimiento por 5.00 |
| Faltante | -5.00 | 1 movimiento por 5.00 |

## Integridad

Las variaciones observadas en la fase consolidada coinciden con lo autorizado:

- `caja_turno`: +2.
- `movimiento_caja`: +4.
- `caja_idempotencia`: +4.

Sin cambios en historicos, ventas, pagos, inventario ni clientes.
