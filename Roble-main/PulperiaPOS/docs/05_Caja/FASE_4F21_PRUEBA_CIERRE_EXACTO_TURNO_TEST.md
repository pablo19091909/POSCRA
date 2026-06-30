# Fase 4F.21 - Prueba controlada de cierre exacto de turno Test

Fecha/hora UTC: 2026-06-29 23:15:15 UTC

## Alcance

Se ejecuto un cierre real por POS.Api del turno existente de `CAJA_PRINCIPAL_TEST` en `Environment=Test`.

La prueba fue limitada a:

- cerrar el turno Test abierto;
- completar campos de cierre en `caja_turno`;
- registrar una idempotencia `CerrarTurno` en estado `Completada`;
- validar reintentos y bloqueos posteriores.

No se crearon ventas, ingresos, retiros, ajustes, reversas, nuevos turnos ni movimientos de diferencia de cierre.

## Linea base previa

- `Environment=Test` validado.
- Escrituras de ventas apagadas.
- Turno abierto Test: 1.
- Turnos `EnCierre` o `Cerrado` antes de la prueba: 0.
- Fondo inicial: 1000.00.
- Ingresos de caja del turno: 2 por 501.00.
- Retiros de caja del turno: 2 por 1300.00.
- Ventas en efectivo del turno: 0.
- Diferencias de cierre previas: 0.
- Efectivo esperado previo: 201.00.

## Activacion temporal

`EnableCajaApiWrite` se habilito solo como variable de entorno del proceso de POS.Api usado para la prueba.

Los archivos de configuracion permanecieron con:

- `EnableCajaApiWrite=false`;
- `EnableVentasApiWrite=false`;
- `RequiredDatabaseEnvironment=Test`;
- `BlockWritesUnlessDatabaseEnvironmentMatches=true`.

No se documentaron tokens, llaves, connection strings, usuarios, identificadores internos ni rowVersion.

## Resultado del cierre exacto

Se ejecuto `POST /api/caja/turnos/{id}/cerrar` por POS.Api con efectivo contado `201.00`.

Resultado:

- HTTP 200.
- Estado final: `Cerrado`.
- Efectivo esperado: 201.00.
- Efectivo contado: 201.00.
- Diferencia: 0.00.
- Fecha de cierre UTC presente.
- `CierreDiferencia` creado: no.
- Resumen:
  - `FondoInicial`: 1 por 1000.00.
  - `IngresoCaja`: 2 por 501.00.
  - `RetiroCaja`: 2 por 1300.00.

## Resultado final agregado

- Turno Test total: 1.
- Turno abierto: 0.
- Turno `EnCierre`: 0.
- Turno cerrado: 1.
- Campos de cierre completos: 1.
- Usuario de cierre valido: 1.
- Movimientos de caja totales del turno: 5.
- `CierreDiferencia`: 0.
- Reversas: 0.
- Movimientos huerfanos: 0.
- Efectivo esperado recalculado desde movimientos: 201.00.

## Integridad historica

Los conteos agregados posteriores fueron:

- `ingreso_caja`: 9.
- `retiro_caja`: 6.
- `cierre_caja`: 15.
- `ventas`: 1948.
- `venta_pago`: 10.
- `venta_idempotencia`: 10.
- Stock agregado de inventario: 3296.00.
- Saldo agregado de clientes: -2957962.50.
- Cliente General: 1.

Estos valores se mantuvieron alineados con la linea base de la prueba; no hubo escrituras en tablas historicas ni datos de negocio fuera del cierre de turno autorizado.

## Validacion tecnica

- `/health`: HTTP 200.
- `/health/database`: HTTP 200.
- `/api/system/version`: HTTP 200.
- WPF compilo sin errores.
- POS.Api compilo sin errores.
- Solucion completa compilo sin errores.

## Resultado

Fase 4F.21 aprobada.

El turno Test quedo cerrado de forma exacta, idempotente y sin diferencia de cierre.

