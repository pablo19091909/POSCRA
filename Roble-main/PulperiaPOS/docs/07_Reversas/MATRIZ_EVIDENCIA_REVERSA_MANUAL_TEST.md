# Matriz de evidencia reversa manual Test

Fecha UTC: 2026-07-02T01:46:29Z

| Area | Evidencia | Resultado |
| --- | --- | --- |
| Health API | `/health`, `/health/database`, `/api/system/version` | 200 |
| R2 | Cinco ciclos estables previos | Aprobado |
| Ambiente | `Environment=Test`, `writes_allowed_for_testing=1` | Confirmado |
| URL local | `https://localhost:7046` | Usada |
| Apertura WPF | Turno Test con fondo `1000.00` | Exitosa |
| Caja idempotencia apertura | `AbrirTurno` completada | +1 |
| Venta WPF | Factura `2002`, total `10.00` | Exitosa |
| Venta idempotencia | Estado `Completada` | +1 |
| Pago | `Efectivo`, `Registrado` | +1 |
| Movimiento caja venta | `VentaEfectivo` | +1 |
| Inventario venta | Stock 90 -> 89 | Correcto |
| Reversa WPF | Reversa total de factura `2002` | Exitosa |
| Reversa persistente | `venta_reversa` | +1 |
| Movimiento caja reversa | `Reversa` | +1 |
| Inventario reversa | Stock 89 -> 90 | Correcto |
| Reversas huerfanas | Conteo 0 | Correcto |
| Cierre WPF | Cierre exacto `1000.00` | Exitoso |
| Diferencia cierre | `0.00` | Correcto |
| Turno final | `Cerrado` | Correcto |
| Turnos abiertos | 0 | Correcto |
| Turnos EnCierre | 0 | Correcto |
| Ingresos historicos | Sin cambio | Correcto |
| Retiros historicos | Sin cambio | Correcto |
| `cierre_caja` | Sin cambio | Correcto |
| Borrado fisico | No usado | Confirmado |
| Fallback SQL escritura | No usado | Confirmado |
| Dual write | No usado | Confirmado |
| Impresion historica | No usada en venta API | Confirmado |
| Incidencia XAML | Literales `` `r`n `` removidos en acciones | Corregida |

## Resultado

La prueba manual consolidada de reversa WPF sobre venta efectiva API y cierre exacto de Caja API queda aprobada para ambiente Test.

