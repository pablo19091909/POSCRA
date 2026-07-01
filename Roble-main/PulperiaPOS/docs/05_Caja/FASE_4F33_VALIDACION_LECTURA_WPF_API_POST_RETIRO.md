# FASE 4F.33 - Validacion lectura WPF/API post retiro

Fecha UTC: 2026-06-30

## Alcance

Validacion ampliada, no destructiva, posterior al retiro sintetico registrado desde `RetirosCajaPage` por Caja API.

No se ejecutaron escrituras. No se crearon turnos, ingresos, retiros, cierres, movimientos, idempotencias, ventas, pagos, ajustes ni reversas.

## Flags

Durante toda la fase:

- `UseCajaApiRead=true`.
- `UseCajaApiOpenWrite=false`.
- `UseCajaApiIngresoWrite=false`.
- `UseCajaApiRetiroWrite=false`.
- `UseCajaApiCierreWrite=false`.
- `UseVentasApiWrite=false`.
- `EnableCajaApiWrite=false`.
- `EnableVentasApiWrite=false`.
- `EnableLegacyHashUpgrade=false`.

## Linea base

Lecturas agregadas confirmaron:

- Turno abierto `CAJA_PRINCIPAL_TEST`: 1.
- Turnos `EnCierre`: 0.
- Idempotencias `EnProceso`: 0.
- Idempotencias fallidas del turno: 0.
- `ingreso_caja`: 9.
- `retiro_caja`: 6.
- `cierre_caja`: 15.
- `ventas`: 1948.
- `venta_pago`: 10.
- `venta_idempotencia`: 10.
- Stock agregado: 3296.00.
- Saldo agregado clientes: -2957962.50.
- Fondo inicial: 1000.00.
- Ingresos Caja API del turno: 100.00.
- Retiros Caja API del turno: 100.00.
- Efectivo esperado: 1000.00.
- Fondo inicial del turno: 1 movimiento.
- Ingreso API de 100.00: 1 movimiento.
- Retiro API de 100.00: 1 movimiento.
- Ajustes del turno: 0.
- Reversas del turno: 0.
- CierreDiferencia del turno abierto: 0.

## Endpoints y seguridad

POS.Api fue levantada temporalmente en modo solo lectura.

Health checks:

- `/health`: HTTP 200.
- `/health/database`: HTTP 200.
- `/api/system/version`: HTTP 200.

GET protegidos sin token:

- `/api/caja/turnos/abierto`: HTTP 401.
- `/api/caja/turnos/{id}/pre-cierre`: HTTP 401.
- `/api/caja/turnos/{id}/movimientos`: HTTP 401.

No se ejecuto ningun `POST`, `PUT`, `PATCH` ni `DELETE`.

## Validacion visual WPF

El operador confirmo validacion visual con API disponible en:

- `IngresoCajaPage`.
- `RetirosCajaPage`.
- `CierreCajaPage`.

Tambien confirmo validacion con API caida en las pantallas relevantes, con mensaje seguro y sin datos tecnicos.

## Limitacion detectada

La revision estatica confirma que `CierreCajaPage` todavia usa rutas historicas (`CajaHelper`, `DBConnection` y `RawPrinterHelper`) para cierre y consulta historica. En esta fase no se modifico codigo, por lo que la migracion de lectura/pre-cierre de `CierreCajaPage` queda pendiente.

`IngresoCajaPage` y `RetirosCajaPage` tienen lectura/resumen por `CajaApiClient` cuando `UseCajaApiRead=true`, pero conservan rutas historicas para escritura cuando los flags API de escritura estan apagados.

## API no disponible

Se detuvo POS.Api de forma controlada. El operador valido el escenario de API caida.

Confirmaciones:

- No se mostraron secretos.
- No se mostro stack trace.
- No se ejecuto escritura.
- No se registro fallback destructivo.
- WPF continuo usable.

## Integridad final

La comparacion final mantuvo los mismos valores de la linea base:

- Estado del turno: Abierto.
- Fondo inicial: 1000.00.
- Ingresos: 100.00.
- Retiros: 100.00.
- Efectivo esperado: 1000.00.
- Sin cambios en tablas historicas, ventas, pagos, idempotencia de ventas, inventario agregado ni saldo agregado.

## Compilacion

- WPF: compilacion correcta, 0 errores.
- POS.Api: compilacion correcta, 0 errores.

## Restauracion

POS.Api temporal fue detenida al finalizar y el puerto local configurado quedo libre.

## Recomendacion

Continuar con Fase 4F.34: auditoria y preparacion de `CierreCajaPage` para lectura/pre-cierre desde Caja API, sin habilitar escritura de cierre todavia.
