# Checklist previo migracion 007 - soporte ventas transaccionales

Fecha de preparacion: 2026-06-26 UTC

## Objetivo

Confirmar las condiciones minimas antes de aplicar `database/migrations/007_SoporteVentasTransaccionales.sql`.

Este checklist no ejecuta cambios por si mismo. La migracion 007 debe aplicarse solo con aprobacion explicita, ventana controlada y respaldo validado.

## Confirmaciones obligatorias

- [ ] Existe respaldo completo reciente de Azure SQL.
- [ ] Se confirmo Point-in-Time Restore disponible y vigente.
- [ ] Se identifico la hora UTC exacta inmediatamente anterior a la ventana de migracion.
- [ ] Se ejecuto diagnostico previo de solo lectura.
- [ ] `dbo.ventas.factura` sigue siendo clave estable y sin duplicados.
- [ ] `dbo.DetalleVenta.factura` mantiene integridad contra `dbo.ventas.factura`.
- [ ] No existen previamente `dbo.venta_idempotencia`, `dbo.venta_pago` ni `dbo.venta_auditoria`, o se entiende su estado actual.
- [ ] Los metodos historicos de `dbo.ventas.metodo_pago` fueron revisados con consulta agregada/distinta.
- [ ] El valor historico de donacion confirmado es `Donación`.
- [ ] El script 007 incluye `Donación` en `CK_venta_pago_metodo`.
- [ ] No se aplicaran scripts de rollback preventivos.
- [ ] POS.Api no tiene endpoints de ventas transaccionales productivos activos durante esta preparacion.
- [ ] WPF no sera modificado durante la migracion.
- [ ] Se cuenta con responsable presente para detener la ejecucion si aparece cualquier error.

## Validacion estatica del script

- [ ] El script es aditivo.
- [ ] El script crea solo tablas de soporte: `venta_idempotencia`, `venta_pago`, `venta_auditoria`.
- [ ] El script no modifica `dbo.ventas`.
- [ ] El script no modifica `dbo.DetalleVenta`.
- [ ] El script no modifica `dbo.inventario`.
- [ ] El script no modifica `dbo.cliente`.
- [ ] El script no inserta, actualiza ni borra datos historicos.
- [ ] Los montos usan `DECIMAL`, no `FLOAT`.
- [ ] `venta_pago.monto` exige valor positivo cuando existe un pago.
- [ ] `venta_pago.monto_recibido` permite `NULL` para metodos no efectivo.
- [ ] `venta_pago.vuelto` permite `NULL` para metodos no efectivo y, si existe, no puede ser negativo.

## Validacion posterior esperada

Despues de aplicar la migracion, ejecutar solo:

`database/diagnostics/007_ValidacionPostMigracionSoporteVentas.sql`

Debe confirmar:

- Las tres tablas de soporte existen.
- Las llaves foraneas existen.
- Los indices esperados existen.
- `CK_venta_pago_metodo` incluye `Donación`.
- `venta_pago.vuelto` permite `NULL`.
- `venta_pago.monto` es positivo.
- Los totales historicos de `ventas` y `DetalleVenta` siguen estables.

## Condicion de no avance

No aplicar la migracion si falta respaldo, falta confirmacion de PITR, el diagnostico previo reporta inconsistencias, o el script local no coincide con estas reglas.
