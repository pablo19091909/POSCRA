# Fase 4F.3A - Correccion indice filtrado migracion 008

Fecha/hora UTC: 2026-06-28 12:11:14 UTC.

## Alcance

Esta fase fue solo de correccion estatica de scripts y documentacion. No se ejecuto la migracion 008, no se ejecuto rollback y no se ejecuto SQL de escritura contra la base.

## Error original

La migracion `database/migrations/008_CajaTurnosYMovimientos.sql` fallo con error de sintaxis SQL Server cerca de `OR`.

Bloque original:

```sql
CREATE INDEX IX_movimiento_caja_factura_pago
    ON dbo.movimiento_caja(factura, pago_id)
    WHERE factura IS NOT NULL OR pago_id IS NOT NULL;
```

## Causa tecnica

El predicado filtrado usaba `OR`. Para mantener compatibilidad clara con SQL Server y evitar predicados filtrados no soportados por el motor en este contexto, se reemplazo por indices filtrados simples.

## Alternativas evaluadas

| Alternativa | Resultado |
| --- | --- |
| Dos indices filtrados separados | Seleccionada. Facilita busquedas independientes por factura y por pago. |
| Un indice compuesto no filtrado | Descartada. Indexaria filas sin factura ni pago y seria menos preciso para consultas independientes. |
| Indices no filtrados independientes | Descartada. Mas simple, pero menos eficiente al incluir valores nulos. |

## Alternativa seleccionada

Se crearon dos indices filtrados separados:

```sql
IX_movimiento_caja_factura
    ON dbo.movimiento_caja(factura)
    WHERE factura IS NOT NULL
```

```sql
IX_movimiento_caja_pago
    ON dbo.movimiento_caja(pago_id)
    WHERE pago_id IS NOT NULL
```

La razon es que las consultas futuras esperadas son independientes:

- consultar movimientos asociados a una venta por `factura`;
- consultar movimientos asociados a un pago por `pago_id`.

## Archivos modificados

- `database/migrations/008_CajaTurnosYMovimientos.sql`
- `database/diagnostics/008_DiagnosticoCajaTurnosYMovimientos.sql`
- `database/diagnostics/008_ValidacionPostMigracionCaja.sql`
- `docs/05_Caja/FASE_4F3_MIGRACION_CAJA_TURNOS_Y_MOVIMIENTOS_APLICADA.md`

No se modifico rollback porque sigue bloqueandose si existen turnos o movimientos y no toca tablas historicas.

## Indices finales esperados

- `UX_caja_turno_un_abierto_por_caja`
- `IX_caja_turno_estado_apertura`
- `IX_movimiento_caja_turno_fecha`
- `IX_movimiento_caja_factura`
- `IX_movimiento_caja_pago`
- `UX_movimiento_caja_pago_efectivo`
- `IX_movimiento_caja_correlacion`

## Validacion estatica

- No queda `OR` dentro de ningun `CREATE INDEX ... WHERE`.
- No se agrego DML ni backfill.
- No se agrego `ALTER` sobre tablas historicas.
- No hay nombres duplicados de `CREATE INDEX`.
- Los objetos nuevos siguen condicionados por `OBJECT_ID` o validacion de indice por nombre.
- El script sigue siendo aditivo e idempotente.

## Estado de base

No se ejecuto SQL de escritura en esta fase. Las tablas 008 deben seguir sin existir hasta una nueva ejecucion controlada:

- `dbo.caja_turno`: pendiente.
- `dbo.movimiento_caja`: pendiente.

## Siguiente intento controlado

Para reintentar la migracion se requiere aprobacion explicita nueva. El texto exacto recomendado es:

```text
Autorizo ejecutar la Fase 4F.3B: reintento controlado de la migracion 008 corregida en la base Environment=Test, sin backfill, sin crear turnos ni movimientos, y con ventas API apagadas.
```
