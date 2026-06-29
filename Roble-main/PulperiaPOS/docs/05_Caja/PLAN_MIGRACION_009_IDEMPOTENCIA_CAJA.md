# Plan migracion 009 Idempotencia Caja

## Fase 4F.8

Diseno, scripts 009 y revision estatica. No ejecutar scripts.

## Fase 4F.9

Ejecutar solo diagnostico:

```text
database/diagnostics/009_DiagnosticoIdempotenciaCaja.sql
```

Validar que no existe `caja_idempotencia`, que no hay conflictos de nombres y que el estado de caja sigue controlado.

## Fase 4F.10

Aplicar controladamente:

```text
database/migrations/009_IdempotenciaCajaApi.sql
```

Solo en Test, sin backfill, sin registros de prueba y sin tocar tablas historicas.

## Fase 4F.11

Integrar idempotencia real en `POST /api/caja/ingresos`, manteniendo `EnableCajaApiWrite=false`.

## Fase 4F.12

Ejecutar una prueba unica de ingreso sintetico con `Idempotency-Key` en Test.

## Rollback

El rollback:

- no se ejecuta en 4F.8;
- se bloquea si existen registros en `caja_idempotencia`;
- no toca `movimiento_caja`, `caja_turno`, ventas ni tablas historicas;
- requiere validacion manual.

## Criterio para permitir primer ingreso real

- Tabla `caja_idempotencia` creada y validada.
- Endpoint exige `Idempotency-Key`.
- Misma key con mismo request no duplica movimiento.
- Misma key con request distinto devuelve `409`.
- `EnableCajaApiWrite` solo se activa temporalmente en Test.
