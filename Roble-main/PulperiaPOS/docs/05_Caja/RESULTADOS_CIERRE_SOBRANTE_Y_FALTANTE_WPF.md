# Resultados cierre sobrante y faltante WPF

Fecha UTC: 2026-07-01 02:46:50 UTC

## Turno A - Sobrante

| Campo | Resultado |
| --- | ---: |
| Fondo inicial | 1,000.00 |
| Efectivo esperado | 1,000.00 |
| Efectivo contado | 1,005.00 |
| Diferencia | 5.00 |
| Movimiento `CierreDiferencia` | 1 |
| Monto del movimiento | 5.00 |
| Idempotencia `CerrarTurno Completada` | 1 |
| Estado final | Cerrado |

El sobrante conserva el signo positivo en `caja_turno.diferencia`. El movimiento `CierreDiferencia` usa monto positivo absoluto.

## Turno B - Faltante

| Campo | Resultado |
| --- | ---: |
| Fondo inicial | 1,000.00 |
| Efectivo esperado | 1,000.00 |
| Efectivo contado | 995.00 |
| Diferencia | -5.00 |
| Movimiento `CierreDiferencia` | 1 |
| Monto del movimiento | 5.00 |
| Idempotencia `CerrarTurno Completada` | 1 |
| Estado final | Cerrado |

El faltante conserva el signo negativo en `caja_turno.diferencia`. El movimiento `CierreDiferencia` usa monto positivo absoluto.

## Validaciones comunes

- Observacion requerida en ambos cierres con diferencia.
- Confirmacion explicita desde WPF.
- No se repitio el cierre manualmente.
- No hubo idempotencias `EnProceso`.
- No hubo idempotencias `Fallida`.
- No se crearon ajustes ni reversas.
- No se modifico `cierre_caja`.
- No hubo SQL historico, dual write ni impresion historica.
