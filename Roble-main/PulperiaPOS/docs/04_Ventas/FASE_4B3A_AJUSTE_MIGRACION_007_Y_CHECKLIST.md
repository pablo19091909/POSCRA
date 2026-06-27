# Fase 4B.3A - Ajuste no ejecutado de migracion 007 y checklist

Fecha/hora UTC: 2026-06-26

## Alcance

Se ajusto la preparacion tecnica de la migracion 007 sin ejecutar scripts SQL contra Azure SQL.

No se crearon tablas, no se modificaron datos, no se aplicaron migraciones y no se ejecuto rollback.

## Validacion agregada de metodos de pago

Consulta ejecutada: solo lectura, agregada/distinta sobre `ventas.metodo_pago`.

Resultado relevante:

| Metodo | Total |
| --- | ---: |
| Donación | 25 |
| Efectivo | 1025 |
| Saldo Cliente | 488 |
| Sinpe | 240 |
| Tarjeta | 103 |

El valor exacto historico para donacion es `Donación`.

## Decision tecnica

Se mantiene `CHECK` explicito en `venta_pago.metodo_pago` para la fase actual.

No se creo catalogo de metodos de pago porque la migracion 007 sigue siendo infraestructura minima y aditiva para soportar ventas transaccionales futuras. Un catalogo puede evaluarse despues, cuando se definan endpoints y reglas finales de ventas API.

## Ajustes realizados al script 007

- `CK_venta_pago_metodo` ahora incluye el valor exacto `Donación`.
- `venta_pago.vuelto` ahora permite `NULL`.
- `venta_pago.monto` exige valor positivo.
- `venta_pago.monto_recibido` permite `NULL` y, si existe, no puede ser negativo.
- `venta_pago.vuelto` permite `NULL` y, si existe, no puede ser negativo.
- Se mantuvieron tipos `DECIMAL` para valores monetarios.

## Validacion estatica

La revision estatica debe confirmar antes de ejecutar la migracion:

- No hay `INSERT`, `UPDATE` ni `DELETE` sobre datos historicos.
- No hay cambios sobre `ventas`, `DetalleVenta`, `inventario`, `cliente` ni caja.
- Las tablas nuevas son solo de soporte.
- La validacion posterior revisa explicitamente `Donación`, `vuelto NULL` y `monto > 0`.

## Checklist previo

Se creo `docs/04_Ventas/CHECKLIST_PREVIO_MIGRACION_007.md`.

El checklist exige confirmar respaldo completo, Point-in-Time Restore y hora UTC previa a la ventana de migracion antes de aplicar 007.

## Scripts revisados o modificados

- `database/migrations/007_SoporteVentasTransaccionales.sql`
- `database/diagnostics/007_ValidacionPostMigracionSoporteVentas.sql`
- `database/rollback/007_SoporteVentasTransaccionales_rollback.sql` revisado, sin cambios requeridos.

## Base de datos

No se ejecuto el script 007.

No se modifico estructura, tablas, datos historicos, usuarios, inventario, ventas, caja ni clientes.

Las tablas de soporte seguian sin existir al momento de la validacion previa.

## Riesgos pendientes

- Aun falta ejecutar la migracion 007 en una ventana controlada.
- El rollback solo debe usarse si las tablas de soporte estan vacias.
- La decision de catalogo de metodos de pago queda pendiente para una fase posterior.
- Las reglas finales de pagos deben validarse cuando se implemente el endpoint transaccional de ventas.

## Recomendacion

Repetir Fase 4B.3 despues de confirmar respaldo, PITR, checklist completo y aprobacion explicita para ejecutar la migracion 007.
