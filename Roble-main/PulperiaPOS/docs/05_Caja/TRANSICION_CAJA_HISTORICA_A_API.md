# Transicion de caja historica a API

## Situacion actual

- `ingreso_caja` guarda ingresos manuales con usuario como texto.
- `retiro_caja` guarda retiros sin usuario en tabla.
- `cierre_caja` guarda totales agregados, sin turno ni diferencia formal.
- Ventas SQL siguen usando `ventas.metodo_pago` y no crean movimiento de caja.
- Ventas API crean `venta_pago`, pero no movimiento de caja.

## Estrategia recomendada

Recomendada: opcion B.

Mantener tablas historicas intactas y crear `movimiento_caja` solo para operaciones nuevas de API, sin backfill inicial.

Motivos:

- Evita reinterpretar datos historicos incompletos.
- Reduce riesgo contable.
- Permite validar el nuevo modelo en Test.
- Mantiene compatibilidad con reportes actuales.

## Alternativas evaluadas

| Opcion | Decision | Riesgo |
| --- | --- | --- |
| Migrar historicos a `movimiento_caja` | No recomendado ahora | Datos historicos no tienen turno, usuario ni esperado/contado. |
| Mantener historicos y nuevos movimientos API | Recomendado | Requiere reportes mixtos durante transicion. |
| Convertir tablas historicas en vistas | Futuro | Requiere estabilizar API y reportes primero. |

## Endpoints futuros

- `POST /api/caja/turnos/abrir`
- `POST /api/caja/ingresos`
- `POST /api/caja/retiros`
- `POST /api/caja/turnos/{id}/pre-cierre`
- `POST /api/caja/turnos/{id}/cerrar`

## Plan por fases

1. Fase 4F.2: aplicar migracion 008 en Test y validar metadata.
2. Fase 4F.3: implementar servicios internos de caja sin conectar WPF.
3. Fase 4F.4: endpoints de apertura/pre-cierre/cierre en API, bloqueados por permisos.
4. Fase 4F.5: integrar Venta API con `MovimientoCaja` solo para efectivo.
5. Fase 4F.6: migrar ingresos/retiros WPF a API.
6. Fase 4F.7: reportes mixtos y plan de retiro de caja historica.
