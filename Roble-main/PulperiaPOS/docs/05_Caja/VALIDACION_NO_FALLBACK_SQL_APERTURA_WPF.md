# Validacion no fallback SQL - Apertura WPF

Fecha/hora UTC: 2026-06-30T03:59:18Z

## Resultado

La apertura de turno API desde WPF no usa fallback SQL.

## Evidencia

| Componente | Resultado |
| --- | --- |
| `IngresoCajaPage.AbrirTurnoApi_Click` | Usa `CajaOperationCoordinator` y `CajaApiClient.AbrirTurnoAsync`. |
| `CajaApiClient.AbrirTurnoAsync` | Envia `POST /api/caja/turnos/abrir` con `Idempotency-Key`. |
| `DBConnection` | No se usa en la accion de apertura API. |
| SQL legacy de ingresos | Permanece solo en `RegistrarIngreso_Click`. |
| Retiros/cierre | No fueron modificados ni conectados al coordinador. |

## Evidencia de datos

Durante la prueba de conflicto:

| Conteo | Antes | Despues |
| --- | ---: | ---: |
| `caja_turno` | 4 | 4 |
| `movimiento_caja` | 10 | 10 |
| `caja_idempotencia` | 8 | 8 |

Si hubiera fallback SQL o doble escritura, alguno de estos conteos habria cambiado. No cambiaron.

## Confirmacion

No se ejecutaron `INSERT`, `UPDATE` ni `DELETE` desde WPF para apertura. No se modificaron tablas historicas.
