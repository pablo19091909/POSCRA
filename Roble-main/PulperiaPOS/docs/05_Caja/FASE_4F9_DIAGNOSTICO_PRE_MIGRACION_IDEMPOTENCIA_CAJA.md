# Fase 4F.9 - Diagnostico pre migracion 009 Idempotencia Caja

Fecha/hora UTC: 2026-06-28 18:36:20 UTC.

## Alcance

Se ejecuto unicamente el diagnostico de lectura `database/diagnostics/009_DiagnosticoIdempotenciaCaja.sql`.

No se ejecuto la migracion 009, no se ejecuto rollback, no se creo `caja_idempotencia` y no se modificaron datos.

## Environment

Resultado:

```text
Environment=Test
writes_allowed_for_testing=1
```

Confirmado por diagnostico:

```text
environment_test_ok = 1
```

## Validacion del diagnostico

El script fue revisado antes de ejecutarse.

Confirmado:

- solo contiene `SELECT`;
- consulta metadata y agregados;
- no contiene `CREATE`, `ALTER`, `DROP`, `INSERT`, `UPDATE`, `DELETE`, `MERGE`, `TRUNCATE`, `EXEC` ni `EXECUTE`;
- no ejecuta SQL dinamico;
- no invoca scripts externos.

## Estado actual

Resultados principales:

- `caja_idempotencia_exists = 0`;
- no se encontro otro objeto `dbo` que pueda confundirse con idempotencia de caja;
- `caja_turno_rows = 1`;
- `movimiento_caja_rows = 1`;
- `ingreso_caja_rows = 9`;
- `retiro_caja_rows = 6`;
- `cierre_caja_rows = 15`;
- `ingresos_api = 0`;
- `retiros_api = 0`.

## Objetos existentes revisados

Se reviso metadata de:

- `caja_turno`;
- `movimiento_caja`;
- `venta_idempotencia`;
- indices relacionados;
- checks relacionados;
- FKs relacionadas.

La tabla historica `venta_idempotencia` existe y queda separada. No se modifica ni reutiliza para Caja API.

## Compatibilidad de tipos y relaciones

Compatible:

- `usuario_id int` con `usuario.idUsuario`;
- `idTurno bigint` con `caja_turno.idTurno`;
- `idMovimiento bigint` con `movimiento_caja.idMovimiento`;
- `idempotency_key uniqueidentifier`;
- `request_hash varbinary(32)`;
- fechas UTC con `datetime2(3)`;
- `row_version rowversion`;
- sin `float` ni `real`;
- sin almacenamiento de request completo.

Las FKs nullable hacia `caja_turno` y `movimiento_caja` son compatibles con estados `EnProceso` y `Fallida`. La referencia a `movimiento_caja` puede permanecer `NULL` hasta que la operacion complete.

## Revision de migracion 009

La migracion `database/migrations/009_IdempotenciaCajaApi.sql` fue revisada de forma estatica.

Confirmado:

- es aditiva;
- es idempotente por `IF OBJECT_ID(...) IS NULL`;
- usa transaccion;
- crea solo `dbo.caja_idempotencia`;
- no toca `venta_idempotencia`;
- no toca tablas historicas;
- no hace backfill;
- no crea registros;
- no modifica el turno Test;
- no crea ingresos API;
- no activa flags;
- no cambia permisos ni endpoints;
- no usa SQL dinamico;
- no usa indices filtrados con `OR`;
- restringe operaciones aprobadas;
- restringe estados aprobados;
- exige hash de 32 bytes;
- bloquea duplicidad por `usuario_id + operacion + idempotency_key`.

## Ajuste aplicado al script

Hallazgo tecnico:

La version inicial tenia un indice no unico sobre `idMovimiento`. Para reforzar que un mismo movimiento no pueda quedar referenciado por multiples registros de idempotencia, se ajusto a:

```text
UX_caja_idempotencia_movimiento
```

con filtro:

```text
idMovimiento IS NOT NULL
```

Este ajuste fue aplicado al archivo preparado. La migracion no fue ejecutada.

## Semantica de estados

Politica aprobada para Caja API V1:

- nueva key: `EnProceso`;
- operacion financiera exitosa: `Completada` con referencia final;
- misma key y mismo request completado: devuelve resultado seguro sin crear duplicado;
- misma key con request distinto: `409 Conflict`;
- key en `EnProceso`: `409 Conflict` seguro;
- error antes de completar: rollback completo preferido;
- registros `EnProceso` abandonados requeriran politica futura de recuperacion por antiguedad, sin implementarla todavia.

## Revision de rollback

El rollback `database/rollback/009_IdempotenciaCajaApi_rollback.sql` fue revisado y no se ejecuto.

Confirmado:

- no toca `movimiento_caja`;
- no toca `caja_turno`;
- no toca `venta_idempotencia`;
- no toca tablas historicas;
- se bloquea si `caja_idempotencia` contiene registros;
- solo elimina el objeto 009 si la tabla existe y esta vacia;
- requiere validacion manual.

## Hallazgos

| Hallazgo | Clasificacion | Decision |
| --- | --- | --- |
| Diagnostico era seguro de solo lectura | Aprobado sin ajustes | Ejecutado |
| Tabla `caja_idempotencia` no existe | Aprobado sin ajustes | Condicion esperada |
| FKs nullable propuestas son compatibles | Aprobado sin ajustes | Mantener |
| Indice de `idMovimiento` debia ser unico | Requiere ajuste de script | Ajustado |
| Politica para `EnProceso` | Requiere decision funcional | `409 Conflict` para V1 |
| Recuperacion de `EnProceso` abandonado | Requiere decision futura | No bloquea migracion 009 |

## Decision final

```text
Aprobado con ajustes
```

La migracion 009 queda tecnicamente aprobada para aplicarse en Test en una fase posterior, usando la version ajustada del script.

## Siguiente fase

Fase 4F.10:

Aplicar controladamente `database/migrations/009_IdempotenciaCajaApi.sql` en Test, sin backfill, sin registros de prueba, sin activar flags y validando luego con `database/diagnostics/009_ValidacionPostMigracionIdempotenciaCaja.sql`.
