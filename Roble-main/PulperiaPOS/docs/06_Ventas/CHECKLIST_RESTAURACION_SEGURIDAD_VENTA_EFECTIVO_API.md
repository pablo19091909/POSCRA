# Checklist restauracion seguridad venta efectivo API

Fecha UTC: 2026-07-01 03:27:11 UTC

## Flags finales WPF

Configuracion local de desarrollo WPF:

- `UseCajaApiRead=true`.
- `UseCajaApiOpenWrite=false`.
- `UseCajaApiIngresoWrite=false`.
- `UseCajaApiRetiroWrite=false`.
- `UseCajaApiCierreWrite=false`.
- `UseVentasApiWrite=false`.
- `UseVentasApiEfectivoWrite=false`.

Configuracion versionada WPF:

- escrituras API permanecen apagadas.

## Flags finales POS.Api

Configuracion API:

- `EnableVentasApiWrite=false`.
- `EnableCajaApiWrite=false`.
- `EnableVentasApiEfectivoCajaWrite=false`.
- `EnableLegacyHashUpgrade=false`.

Durante la ventana de prueba, los flags de escritura se activaron solo como variables del proceso temporal de POS.Api.

## API y puerto

- POS.Api fue detenida al finalizar la fase.
- El puerto local quedo libre.

## Compilacion

- POS.Api: compilacion correcta, 0 advertencias, 0 errores.
- WPF: compilacion correcta, 0 advertencias, 0 errores.
- Solucion completa previa: compilacion correcta, 0 advertencias, 0 errores.

## Git

No fue posible ejecutar `git status` desde esta terminal porque `git` no esta disponible en el PATH.

## Evidencia Test conservada

Se conserva en la base Test:

- turno Test abierto;
- movimiento `FondoInicial`;
- venta sintetica efectiva;
- detalle de venta;
- pago;
- auditoria;
- idempotencia de venta completada;
- movimiento `VentaEfectivo`.

No se ejecuto rollback ni eliminacion de evidencia.

## Seguridad

- No se imprimieron connection strings.
- No se imprimieron contrasenas.
- No se imprimieron tokens.
- No se imprimieron signing keys.
- No se documentaron ids internos de usuario, cliente, producto, venta, pago, turno ni movimientos.
- No se modifico WPF.
- No se modificaron `DBConnection.cs`, `CajaHelper` ni `RawPrinterHelper`.
- No se ejecutaron scripts SQL de escritura.
