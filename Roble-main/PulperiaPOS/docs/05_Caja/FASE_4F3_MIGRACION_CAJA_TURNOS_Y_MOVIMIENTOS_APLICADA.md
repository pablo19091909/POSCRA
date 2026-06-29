# Fase 4F.3 - Migracion 008 de caja, intento controlado

Fecha/hora UTC: 2026-06-28 12:03:57 UTC.

## Resultado ejecutivo

La migracion `database/migrations/008_CajaTurnosYMovimientos.sql` fue ejecutada una sola vez contra la base marcada como `Environment=Test`, pero fallo por error de sintaxis SQL antes de confirmar la transaccion.

No se ejecuto rollback, no se repitio la migracion, no se crearon objetos manualmente y no se aplico script alternativo.

Actualizacion Fase 4F.3A: el script fue corregido estaticamente para reemplazar el indice filtrado incompatible con dos indices filtrados separados. La migracion sigue pendiente de una nueva autorizacion explicita; no ha sido aplicada.

## Validaciones previas

| Validacion | Resultado |
| --- | --- |
| `Environment=Test` | OK |
| `writes_allowed_for_testing=1` | OK |
| `UseVentasApiWrite=false` | OK |
| `EnableVentasApiWrite=false` | OK |
| `caja_turno` inexistente antes de migrar | OK |
| `movimiento_caja` inexistente antes de migrar | OK |
| Script sin DML/backfill/drop ni alteraciones sobre historicos | OK |
| `CAJA_PRINCIPAL_TEST` solo como codigo futuro, sin turno automatico | OK |

Health checks previos:

| Endpoint | Resultado |
| --- | --- |
| `/health` | HTTP 200 |
| `/health/database` | HTTP 200 |
| `/api/system/version` | HTTP 200 |

## Script ejecutado

`database/migrations/008_CajaTurnosYMovimientos.sql`

Resultado:

```text
Fallo controlado: Incorrect syntax near the keyword 'OR'.
```

Ubicacion identificada:

```sql
CREATE INDEX IX_movimiento_caja_factura_pago
    ON dbo.movimiento_caja(factura, pago_id)
    WHERE factura IS NOT NULL OR pago_id IS NOT NULL;
```

Interpretacion tecnica: SQL Server no acepto ese predicado con `OR` en el indice filtrado. La migracion debe ajustarse antes de reintentar.

## Objetos creados

Despues del fallo:

| Objeto | Existe |
| --- | ---: |
| `dbo.caja_turno` | 0 |
| `dbo.movimiento_caja` | 0 |

La transaccion interna no dejo tablas nuevas persistidas.

## Integridad historica antes y despues

Los agregados posteriores coinciden con la linea base previa:

| Indicador | Antes | Despues |
| --- | ---: | ---: |
| ventas | 1925 | 1925 |
| detalles | 5038 | 5038 |
| pagos API | 10 | 10 |
| auditorias API | 10 | 10 |
| idempotencias API | 10 | 10 |
| ingresos caja | 9 | 9 |
| retiros caja | 6 | 6 |
| cierres caja | 15 | 15 |
| productos inventario | 226 | 226 |
| stock agregado | 3375 | 3375 |
| clientes | 167 | 167 |
| saldo agregado en centavos | -291126250 | -291126250 |

No se modifico el cierre historico negativo identificado en Fase 4F.2.

## Estado de tablas nuevas

No existen tablas nuevas porque la migracion fallo antes del commit:

- `dbo.caja_turno`: no creada.
- `dbo.movimiento_caja`: no creada.
- Turnos creados: 0.
- Movimientos creados: 0.
- No se creo ningun turno para `CAJA_PRINCIPAL_TEST`.

## Estado de ventas API

- WPF `UseVentasApiWrite=false`.
- POS.Api `EnableVentasApiWrite=false`.
- No se activaron ventas API.
- No se inicio caja API.
- `POST /api/ventas` sin token permanece bloqueado por autenticacion con HTTP 401; no se realizo prueba autenticada para no manejar credenciales ni JWT en esta fase.

## Compilacion y salud tecnica

- Solucion completa: 0 errores.
- WPF: 0 errores.
- POS.Api: 0 errores.
- `/health`: HTTP 200.
- `/health/database`: HTTP 200.
- `/api/system/version`: HTTP 200.

## Riesgos pendientes

- La migracion 008 requiere ajuste de script antes de un nuevo intento.
- El indice filtrado con `OR` debe reemplazarse por una alternativa compatible con SQL Server.
- No iniciar implementacion de servicios de caja hasta que `caja_turno` y `movimiento_caja` existan correctamente.

## Recomendacion

Fase 4F.3A fue ejecutada como ajuste no ejecutado del script 008:

1. Se corrigio el indice filtrado incompatible.
2. Se valido estaticamente que no haya backfill ni cambios historicos.
3. Queda pendiente reintentar la migracion solo con aprobacion explicita posterior.
