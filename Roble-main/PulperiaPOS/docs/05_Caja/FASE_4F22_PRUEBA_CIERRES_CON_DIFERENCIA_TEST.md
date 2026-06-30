# Fase 4F.22 - Prueba controlada de cierres con diferencia en Test

Fecha/hora UTC: 2026-06-29 23:34:01 UTC

## Alcance

Se ejecuto una validacion real por POS.Api en `Environment=Test` usando exclusivamente `CAJA_PRINCIPAL_TEST`.

La fase autorizo dos turnos sinteticos secuenciales:

- Turno A: cierre con sobrante.
- Turno B: cierre con faltante.

No se modifico WPF ni se usaron escrituras SQL manuales para crear o cerrar turnos.

## Linea base inicial

- `Environment=Test`: confirmado.
- `writes_allowed_for_testing=1`: confirmado.
- Turnos Test totales previos: 1.
- Turnos Test abiertos previos: 0.
- Turnos Test `EnCierre` previos: 0.
- Turnos Test cerrados previos: 1.
- Turno exacto anterior cerrado: 1.
- Idempotencias `EnProceso`: 0.
- Idempotencias `Fallida`: 0.
- Movimientos caja totales previos: 5.
- Movimientos `CierreDiferencia` previos: 0.
- Idempotencias `CerrarTurno Completada` previas: 1.

Tablas y agregados historicos previos:

- `ingreso_caja`: 9.
- `retiro_caja`: 6.
- `cierre_caja`: 15.
- `ventas`: 1948.
- `venta_pago`: 10.
- `venta_idempotencia`: 10.
- Inventario agregado: 3296.00.
- Saldo agregado de clientes: -2957962.50.

## Activacion temporal

`EnableCajaApiWrite` se activo solo como variable de entorno del proceso de POS.Api usado para la prueba.

Los archivos versionados permanecieron con:

- `EnableCajaApiWrite=false`.
- `EnableVentasApiWrite=false`.
- `EnableLegacyHashUpgrade=false`.
- `RequiredDatabaseEnvironment=Test`.
- `BlockWritesUnlessDatabaseEnvironmentMatches=true`.

## Turno A - Sobrante

Se abrio un turno nuevo por `POST /api/caja/turnos/abrir` con fondo inicial `1000.00`.

Validaciones:

- apertura HTTP 200;
- estado inicial `Abierto`;
- un unico movimiento inicial `FondoInicial` por `1000.00`;
- pre-cierre inicial `1000.00`;
- rowVersion valido obtenido desde endpoint de lectura, sin imprimir su valor.

Se cerro por `POST /api/caja/turnos/{id}/cerrar` con efectivo contado `1005.00`.

Resultado:

- cierre HTTP 200;
- estado final `Cerrado`;
- efectivo esperado `1000.00`;
- efectivo contado `1005.00`;
- diferencia `+5.00`;
- `CierreDiferencia` creado: si;
- resumen del cierre: `FondoInicial` 1 por `1000.00`.

## Turno B - Faltante

El Turno B se inicio solo despues de confirmar que no existia turno abierto tras cerrar el Turno A.

Se abrio un turno nuevo por `POST /api/caja/turnos/abrir` con fondo inicial `1000.00`.

Validaciones:

- apertura HTTP 200;
- estado inicial `Abierto`;
- un unico movimiento inicial `FondoInicial` por `1000.00`;
- pre-cierre inicial `1000.00`;
- rowVersion valido obtenido desde endpoint de lectura, sin imprimir su valor.

Se cerro por `POST /api/caja/turnos/{id}/cerrar` con efectivo contado `995.00`.

Resultado:

- cierre HTTP 200;
- estado final `Cerrado`;
- efectivo esperado `1000.00`;
- efectivo contado `995.00`;
- diferencia `-5.00`;
- `CierreDiferencia` creado: si;
- resumen del cierre: `FondoInicial` 1 por `1000.00`.

## Resultado final

- Nuevos turnos de la fase: 2.
- Turnos Test abiertos finales: 0.
- Turnos Test `EnCierre` finales: 0.
- Turnos Test cerrados finales: 3.
- Movimientos de la fase no autorizados: 0.
- Reversas de la fase: 0.
- Movimientos huerfanos de la fase: 0.
- Idempotencias `CerrarTurno Completada` de la fase: 2.
- Idempotencias `EnProceso`: 0.
- Idempotencias `Fallida`: 0.

## Compilacion y health checks

- `/health`: HTTP 200.
- `/health/database`: HTTP 200.
- `/api/system/version`: HTTP 200.
- WPF: compilacion correcta, 0 errores.
- POS.Api: compilacion correcta, 0 errores.
- Solucion completa: compilacion correcta, 0 errores.

## Resultado

Fase 4F.22 aprobada.

