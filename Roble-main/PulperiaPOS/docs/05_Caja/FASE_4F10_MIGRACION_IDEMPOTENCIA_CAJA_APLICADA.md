# Fase 4F.10 - Migracion 009 Idempotencia Caja aplicada

Fecha/hora UTC: 2026-06-28 18:50:51 UTC.

## Environment

Confirmado antes de ejecutar:

```text
Environment=Test
writes_allowed_for_testing=1
```

## Validaciones previas

Flags confirmados apagados:

- `UseVentasApiWrite=false`
- `EnableVentasApiWrite=false`
- `EnableCajaApiWrite=false`

Linea base previa:

| Metrica | Antes |
| --- | ---: |
| `caja_idempotencia` | inexistente |
| `caja_turno` | 1 |
| `movimiento_caja` | 1 |
| `ingreso_caja` | 9 |
| `retiro_caja` | 6 |
| `cierre_caja` | 15 |
| ventas | 1948 |
| pagos | 10 |
| stock agregado | 3296 |
| saldo agregado en centavos | -295796250 |

Se confirmo que el script no contiene DML, backfill, cambios a `venta_idempotencia` ni cambios a tablas historicas.

## Script ejecutado

Se ejecuto una unica vez:

```text
database/migrations/009_IdempotenciaCajaApi.sql
```

Resultado:

```text
Migration 009 executed successfully.
```

No se ejecuto rollback.

## Estructura creada

Se creo `dbo.caja_idempotencia` con:

- PK `PK_caja_idempotencia`;
- FK `FK_caja_idempotencia_usuario`;
- FK `FK_caja_idempotencia_turno`;
- FK `FK_caja_idempotencia_movimiento`;
- `idempotency_key` como `uniqueidentifier`;
- `request_hash` como `varbinary(32)`;
- fechas `datetime2(3)`;
- `row_version`;
- sin `float` ni `real`;
- sin request completo ni datos sensibles.

## Checks

Checks creados:

- `CK_caja_idempotencia_operacion`;
- `CK_caja_idempotencia_estado`;
- `CK_caja_idempotencia_fechas`;
- `CK_caja_idempotencia_completada_referencia`;
- `CK_caja_idempotencia_hash_len`.

Operaciones permitidas:

- `IngresoCaja`;
- `RetiroCaja`;
- `CerrarTurno`;
- `AjusteCaja`;
- `ReversaMovimiento`.

Estados permitidos:

- `EnProceso`;
- `Completada`;
- `Fallida`.

## Indices

Indices creados:

- `UX_caja_idempotencia_usuario_operacion_key`;
- `IX_caja_idempotencia_estado_actualizado`;
- `IX_caja_idempotencia_turno_operacion`;
- `UX_caja_idempotencia_movimiento`;
- `IX_caja_idempotencia_caja_operacion`.

Confirmado el indice unico:

```text
UX_caja_idempotencia_movimiento
WHERE idMovimiento IS NOT NULL
```

No se crearon indices filtrados con `OR`.

## Tabla vacia

Postmigracion:

```text
caja_idempotencia_rows = 0
```

No se crearon registros de prueba.

## Integridad despues

| Metrica | Antes | Despues |
| --- | ---: | ---: |
| `caja_idempotencia` | inexistente | 0 |
| `caja_turno` | 1 | 1 |
| `movimiento_caja` | 1 | 1 |
| `ingreso_caja` | 9 | 9 |
| `retiro_caja` | 6 | 6 |
| `cierre_caja` | 15 | 15 |
| ventas | 1948 | 1948 |
| pagos | 10 | 10 |
| stock agregado | 3296 | 3296 |
| saldo agregado en centavos | -295796250 | -295796250 |

No se detecto actividad externa concurrente durante esta fase.

## Validacion API

Health checks:

- `/health=200`
- `/health/database=200`
- `/api/system/version=200`

`POST /api/caja/ingresos` con permiso y flag apagado respondio `503`, sin crear registros.

## Estado final

Flags finales:

- `UseVentasApiWrite=false`
- `EnableVentasApiWrite=false`
- `EnableCajaApiWrite=false`

POS.Api fue detenida y el puerto `7046` quedo libre.

## Limitaciones pendientes

- Endpoint de ingreso aun bloqueado.
- `Idempotency-Key` aun no integrada al endpoint.
- Sin ingresos API.
- Sin retiros API.
- Sin pre-cierre/cierre con escritura.
- Sin WPF Caja API.
- Sin integracion de ventas API con movimientos de efectivo.
- Sin anulacion o reversa implementada.

## Recomendacion

Avanzar a Fase 4F.11 para integrar idempotencia real en `POST /api/caja/ingresos`, manteniendo `EnableCajaApiWrite=false`, exigiendo `Idempotency-Key` y sin ejecutar ingresos reales todavia.
