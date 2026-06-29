# Fase 4F.8 - Diseno de idempotencia persistente Caja API

Fecha/hora UTC: 2026-06-28 18:26:48 UTC.

## Alcance

Se audito el patron de idempotencia de ventas y se preparo un diseno independiente para Caja API.

No se ejecutaron scripts, no se crearon tablas, no se activaron flags y no se crearon movimientos adicionales.

## Patron observado en ventas

Ventas usa:

- `venta_idempotencia`;
- key por usuario;
- hash SHA-256 del request canonico;
- estados `EnProceso`, `Completada`, `Fallida`;
- transaccion serializable;
- bloqueo de idempotencia durante la operacion;
- respuesta repetida segura cuando la venta ya fue completada;
- conflicto cuando la misma key se reutiliza con request distinto.

El patron reutilizable para caja es conceptual. Caja API no debe acoplarse a `venta_idempotencia`.

## Diferencia con Caja API

Caja requiere una tabla independiente porque sus operaciones no son ventas y deben cubrir:

- ingresos;
- retiros;
- cierre de turno;
- ajustes;
- reversas futuras.

La tabla propuesta es `dbo.caja_idempotencia`.

## Archivos preparados

- `database/diagnostics/009_DiagnosticoIdempotenciaCaja.sql`
- `database/migrations/009_IdempotenciaCajaApi.sql`
- `database/rollback/009_IdempotenciaCajaApi_rollback.sql`
- `database/diagnostics/009_ValidacionPostMigracionIdempotenciaCaja.sql`

Los scripts no fueron ejecutados.

## Codigo preparado

Se agregaron modelos y servicios internos no integrados:

- `CajaIdempotencyOperation`
- `CajaIdempotencyStatus`
- `CajaIdempotencyState`
- `ICajaIdempotencyService`
- `CajaIdempotencyService`
- `ICajaIdempotencyRepository`

No se registraron en DI y no se conectaron a `POST /api/caja/ingresos`.

## Integridad esperada

Al finalizar esta fase debe seguir existiendo:

- un turno abierto `CAJA_PRINCIPAL_TEST`;
- un movimiento `FondoInicial`;
- cero ingresos API;
- cero retiros API;
- cero cierres API;
- `ingreso_caja=9`;
- `retiro_caja=6`;
- `cierre_caja=15`.

## Recomendacion

Avanzar a Fase 4F.9 para ejecutar solo el diagnostico 009 y aprobar tecnicamente la migracion antes de aplicarla.
