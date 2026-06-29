# Fase 4F.3B - Reintento migracion 008 corregida aplicada

Fecha/hora UTC: 2026-06-28 12:36:13 UTC.

## Alcance

Se reintento de forma controlada la migracion corregida `database/migrations/008_CajaTurnosYMovimientos.sql` en la base marcada como `Environment=Test`.

No se creo ningun turno, no se creo ningun movimiento de caja, no se hizo backfill y no se activaron ventas API ni caja API.

## Validaciones previas

| Validacion | Resultado |
| --- | --- |
| `Environment=Test` y `writes_allowed_for_testing=1` | OK |
| `caja_turno` inexistente antes de migrar | OK |
| `movimiento_caja` inexistente antes de migrar | OK |
| `UseVentasApiWrite=false` | OK |
| `EnableVentasApiWrite=false` | OK |
| Script sin `OR` en indices filtrados | OK |
| Script sin DML/backfill | OK |

Health checks previos:

| Endpoint | Resultado |
| --- | --- |
| `/health` | HTTP 200 |
| `/health/database` | HTTP 200 |
| `/api/system/version` | HTTP 200 |

## Resultado de ejecucion

Script ejecutado:

```text
database/migrations/008_CajaTurnosYMovimientos.sql
```

Resultado:

```text
MIGRATION_008_CORRECTED_EXECUTED_OK
```

## Tablas creadas

| Tabla | Existe | Filas |
| --- | ---: | ---: |
| `dbo.caja_turno` | 1 | 0 |
| `dbo.movimiento_caja` | 1 | 0 |

No se creo automaticamente ningun turno para `CAJA_PRINCIPAL_TEST`.

## Estructura validada

### `caja_turno`

Validado:

- PK `PK_caja_turno`.
- Caja logica `caja_codigo`.
- Estado con `Abierto`, `EnCierre`, `Cerrado`, `Anulado`.
- Usuario de apertura y cierre con FK a `usuario`.
- Fechas UTC `apertura_utc`, `cierre_utc`, `creado_utc`, `actualizado_utc`.
- Montos `decimal(18,2)`: fondo inicial, efectivo esperado, efectivo contado y diferencia.
- `row_version` para concurrencia.
- FK opcional a `cierre_caja`.
- Indice unico filtrado `UX_caja_turno_un_abierto_por_caja`.
- Indice `IX_caja_turno_estado_apertura`.

### `movimiento_caja`

Validado:

- PK `PK_movimiento_caja`.
- FK a `caja_turno`.
- FK a `usuario`.
- Referencias nullable a `ventas`, `venta_pago`, `ingreso_caja`, `retiro_caja` y reversa.
- Monto `decimal(18,2)` con CHECK `monto > 0`.
- Moneda `CRC`/`USD`.
- Fecha UTC.
- Tipos compatibles: `FondoInicial`, `VentaEfectivo`, `IngresoCaja`, `RetiroCaja`, `AjustePositivo`, `AjusteNegativo`, `DevolucionEfectivo`, `CierreDiferencia`, `Reversa`.
- Indices:
  - `IX_movimiento_caja_turno_fecha`.
  - `IX_movimiento_caja_factura`.
  - `IX_movimiento_caja_pago`.
  - `UX_movimiento_caja_pago_efectivo`.
  - `IX_movimiento_caja_correlacion`.

Validacion adicional:

- Indices filtrados con `OR`: 0.
- Duplicados de turno abierto: 0.
- Duplicados de movimiento por pago efectivo: 0.

## Integridad historica

Los agregados historicos antes y despues se mantuvieron iguales:

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

El cierre historico negativo detectado previamente no fue modificado.

## Validacion tecnica

- Script postmigracion `database/diagnostics/008_ValidacionPostMigracionCaja.sql`: ejecutado solo lectura.
- Solucion completa: 0 errores.
- WPF: 0 errores.
- POS.Api: 0 errores.
- `/health`: HTTP 200.
- `/health/database`: HTTP 200.
- `/api/system/version`: HTTP 200.

## Estado de flags

- WPF `UseVentasApiWrite=false`.
- POS.Api `EnableVentasApiWrite=false`.

## Riesgos pendientes

- Todavia no existe apertura de turno API.
- Todavia no existe `MovimientoCaja` integrado a ventas API.
- Todavia no existe cierre transaccional por turno.
- Caja historica sigue dependiendo de los flujos WPF hasta migracion posterior.

## Recomendacion

Avanzar a Fase 4F.4 para preparar servicios internos y contratos API de caja en modo no activado: apertura de turno, pre-cierre/cierre, ingresos/retiros futuros y validaciones de permisos, sin conectar aun WPF ni crear movimientos reales.
