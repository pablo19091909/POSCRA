# Fase 4F.16 - Prueba RetiroCaja idempotente en Test

Fecha UTC: 2026-06-29 16:43:03 UTC

## Alcance ejecutado

Se ejecuto una prueba controlada de `POST /api/caja/retiros` en la base marcada como `Environment=Test`.

Se crearon exclusivamente:

- un movimiento `RetiroCaja` principal por `500.00`;
- una idempotencia `RetiroCaja` `Completada` relacionada;
- un movimiento `RetiroCaja` adicional por concurrencia real de `800.00`;
- una idempotencia `RetiroCaja` `Completada` relacionada con la operacion concurrente exitosa.

No se crearon ingresos, ajustes, reversas, cierres, turnos, ventas, pagos ni cambios historicos.

## Linea base previa

Antes de activar Caja API:

- `Environment=Test`: confirmado.
- `writes_allowed_for_testing=1`: confirmado.
- turno abierto `CAJA_PRINCIPAL_TEST`: `1`.
- turnos `EnCierre` o `Cerrado` para la misma caja: `0`.
- movimientos totales: `3`.
- `FondoInicial`: `1`.
- `IngresoCaja`: `2`.
- `RetiroCaja`: `0`.
- idempotencias totales: `2`.
- idempotencias `IngresoCaja Completada`: `2`.
- idempotencias `RetiroCaja`: `0`.
- efectivo esperado: `1501.00`.
- usuarios autorizados existentes: confirmado.

Historicos antes:

- `ingreso_caja=9`;
- `retiro_caja=6`;
- `cierre_caja=15`;
- `ventas=1948`;
- `venta_pago=10`;
- inventario agregado `3296.00`;
- saldo agregado clientes `-2957962.50`.

## Activacion temporal

Se activo `EnableCajaApiWrite=true` solo como variable temporal del proceso de POS.Api.

Se mantuvo:

- `EnableVentasApiWrite=false`;
- `UseVentasApiWrite=false`;
- `EnvironmentSafety.RequiredDatabaseEnvironment=Test`;
- `EnvironmentSafety.BlockWritesUnlessDatabaseEnvironmentMatches=true`.

Health checks:

- `/health=200`;
- `/health/database=200`;
- `/api/system/version=200`.

## Retiro principal

Solicitud autorizada:

- endpoint: `POST /api/caja/retiros`;
- caja: `CAJA_PRINCIPAL_TEST`;
- monto: `500.00`;
- motivo: `Retiro sintetico controlado Fase 4F.16`;
- referencia: `TEST-RETIRO-F4F16`;
- resultado: HTTP `200`;
- respuesta segura: tipo `RetiroCaja`, monto `500.00`, turno presente, fecha UTC presente.

## Idempotencia

Misma key y mismo request:

- resultado: HTTP `200`;
- devolvio resultado equivalente;
- no creo movimiento adicional;
- no creo idempotencia adicional;
- no desconto efectivo nuevamente.

Misma key y request distinto:

- resultado: HTTP `409`;
- no creo movimiento;
- no modifico la operacion original;
- no cambio el pre-cierre.

## Insuficiencia

Luego del retiro principal, el disponible esperado era `1001.00`.

Se intento un retiro mayor al disponible:

- resultado: HTTP `409`;
- no creo movimiento por monto insuficiente;
- no creo idempotencia `Completada`;
- no dejo `EnProceso`;
- no cambio historicos.

## Concurrencia real

Se ejecutaron dos solicitudes HTTP simultaneas con keys distintas y monto `800.00`.

Resultado:

- respuestas: `200` y `409`;
- se creo como maximo un retiro adicional;
- no quedaron dos retiros de `800.00`;
- no quedo efectivo negativo;
- no quedo idempotencia completada para la operacion no creada.

## Estado final

- movimientos totales: `5`;
- movimientos del turno: `5`;
- `FondoInicial`: `1`;
- `IngresoCaja`: `2`;
- `RetiroCaja`: `2`;
- `RetiroCaja 500.00`: `1`;
- `RetiroCaja 800.00`: `1`;
- retiros por monto insuficiente: `0`;
- idempotencias totales: `4`;
- idempotencias `IngresoCaja Completada`: `2`;
- idempotencias `RetiroCaja Completada`: `2`;
- idempotencias `RetiroCaja EnProceso`: `0`;
- idempotencias `RetiroCaja Fallida`: `0`;
- duplicados por usuario + operacion + key: `0`;
- retiros sin idempotencia: `0`;
- idempotencias completadas sin movimiento: `0`;
- efectivo esperado: `201.00`.

## Historicos

Sin cambios:

- `ingreso_caja=9`;
- `retiro_caja=6`;
- `cierre_caja=15`;
- `ventas=1948`;
- `venta_pago=10`;
- inventario agregado `3296.00`;
- saldo agregado clientes `-2957962.50`.

## Restauracion

Estado final:

- `EnableCajaApiWrite=false` en configuracion versionada/local;
- `EnableVentasApiWrite=false`;
- `UseVentasApiWrite=false`;
- POS.Api detenida;
- puerto `7046` libre.

No se ejecuto rollback ni se eliminaron registros. Los movimientos e idempotencias quedan como evidencia Test.

## Limitaciones pendientes

- Sin cierre API real.
- Sin reversas.
- Sin ventas en efectivo integradas a caja.
- Sin WPF Caja API.
- Sin Dolares, Donacion ni pagos combinados.

## Recomendacion

Continuar con Fase 4F.17: validacion operativa de lectura posterior a retiros, revisando turno, movimientos, pre-cierre e idempotencias sin nuevas escrituras.
