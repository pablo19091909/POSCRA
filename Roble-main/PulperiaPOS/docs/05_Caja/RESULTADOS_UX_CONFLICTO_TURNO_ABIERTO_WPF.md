# Resultados UX - Conflicto de turno abierto WPF

Fecha/hora UTC: 2026-06-30T03:59:18Z

## Resultado esperado

Cuando ya existe un turno abierto para `CAJA_PRINCIPAL_TEST`, la accion de apertura por API debe devolver conflicto seguro y mostrar un mensaje claro:

`Ya existe un turno abierto para esta caja.`

## Evidencia automatizada

| Prueba | Resultado |
| --- | --- |
| Turno abierto previo | 1 |
| `POST /api/caja/turnos/abrir` | HTTP 409 |
| Turnos despues | Sin cambios |
| Movimientos despues | Sin cambios |
| Idempotencias despues | Sin cambios |

## Evidencia WPF por codigo

| Elemento | Resultado |
| --- | --- |
| Confirmacion previa | `MessageBox` antes de enviar request. |
| Indicador de procesamiento | `txtAperturaTurnoApiEstado = Caja API: abriendo turno...` |
| Boton bloqueado | `btnAbrirTurnoApi.IsEnabled = false` durante la solicitud. |
| Mensaje conflicto | `Ya existe un turno abierto para esta caja.` |
| Refresco estado API | `CajaApiReadStatusViewHelper.LoadAsync` tras conflicto. |
| Detalles tecnicos | No se muestran. |

## Limitacion

La prueba visual con clic real de operador no fue observada en esta ejecucion. Queda lista para confirmacion manual.
