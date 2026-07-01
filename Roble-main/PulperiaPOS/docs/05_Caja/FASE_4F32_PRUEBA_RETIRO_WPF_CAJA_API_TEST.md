# FASE 4F.32 - Prueba retiro WPF por Caja API en Test

Fecha UTC: 2026-06-30

## Alcance

Se ejecuto una prueba manual controlada de un retiro sintetico desde `RetirosCajaPage` WPF hacia Caja API.

Operacion autorizada:

- Tipo: `RetiroCaja`.
- Monto: 100.00.
- Referencia funcional: `TEST-RETIRO-WPF-F4F32`.
- Caja: `CAJA_PRINCIPAL_TEST`.
- Entorno: Test.

No se ejecuto rollback y el retiro sintetico queda como evidencia Test.

## Linea base nueva

Antes de ejecutar la escritura se confirmo por lecturas agregadas:

- Turno abierto para `CAJA_PRINCIPAL_TEST`: 1.
- Turnos `EnCierre`: 0.
- Idempotencias `EnProceso`: 0.
- `ingreso_caja`: 9.
- `retiro_caja`: 6.
- `cierre_caja`: 15.
- `ventas`: 1948.
- `venta_pago`: 10.
- `venta_idempotencia`: 10.
- Stock agregado inventario: 3296.00.
- Saldo agregado clientes: -2957962.50.
- Fondo inicial del turno: 1000.00.
- Ingresos del turno: 100.00.
- Retiros del turno: 0.00.
- Efectivo esperado: 1100.00.

## Flags

Antes:

- `UseCajaApiRead=true`.
- `UseCajaApiOpenWrite=false`.
- `UseCajaApiIngresoWrite=false`.
- `UseCajaApiRetiroWrite=false`.
- `UseCajaApiCierreWrite=false`.
- `UseVentasApiWrite=false`.
- `EnableCajaApiWrite=false`.
- `EnableVentasApiWrite=false`.
- `EnableLegacyHashUpgrade=false`.

Durante la prueba:

- `UseCajaApiRetiroWrite=true` solo para WPF local.
- `EnableCajaApiWrite=true` solo para el proceso temporal de POS.Api.
- Escrituras de apertura, ingreso, cierre y ventas permanecieron apagadas.

Despues:

- `UseCajaApiRetiroWrite=false`.
- `UseVentasApiWrite=false`.
- POS.Api temporal detenida.

## Validacion de seguridad

- `/health`: HTTP 200.
- `/health/database`: HTTP 200.
- `/api/system/version`: HTTP 200.
- `POST /api/caja/retiros` sin token: HTTP 401.
- Se uso un usuario existente autenticado desde WPF.
- No se documentaron credenciales, tokens, connection strings, hashes ni idempotency keys.

Limitacion: no se completo una prueba visual con usuario sin permiso `Caja.Retirar`.

## Validacion visual WPF

El operador confirmo visualmente:

- Modo Caja API en `RetirosCajaPage`.
- Ejecucion desde WPF real.
- Mensaje de exito: el retiro fue registrado correctamente por Caja API.

Durante el primer intento se detecto que WPF estaba usando una configuracion anterior o una API con escritura apagada. Se corrigio levantando POS.Api temporal con escritura de caja activa y recompilando/copiendo la configuracion local WPF.

## Resultado posterior

Lecturas agregadas posteriores confirmaron:

- Turno abierto para `CAJA_PRINCIPAL_TEST`: 1.
- Turnos `EnCierre`: 0.
- Idempotencias `EnProceso`: 0.
- Idempotencias `RetiroCaja` fallidas: 0.
- `ingreso_caja`: 9.
- `retiro_caja`: 6.
- `cierre_caja`: 15.
- `ventas`: 1948.
- `venta_pago`: 10.
- `venta_idempotencia`: 10.
- Stock agregado inventario: 3296.00.
- Saldo agregado clientes: -2957962.50.
- Fondo inicial: 1000.00.
- Ingresos del turno: 100.00.
- Retiros del turno: 100.00.
- Efectivo esperado: 1000.00.
- Retiros API del turno: 1.
- Total retiros API del turno: 100.00.
- Referencia funcional del retiro autorizado: 1 coincidencia.
- Idempotencias `RetiroCaja Completada`: aumento de 2 a 3.
- Idempotencia `RetiroCaja Completada` reciente: 1.

## Compilacion

- WPF: compilacion correcta, 0 errores.
- POS.Api: compilacion correcta, 0 errores.

## Restauracion

- `UseCajaApiRetiroWrite` fue restaurado a `false` en configuracion local y copia de ejecucion.
- POS.Api temporal fue detenida.
- El puerto local configurado quedo libre.

## Riesgos pendientes

- Cierre WPF por API sigue apagado.
- No hay reversas de movimientos de caja.
- Ventas API en efectivo aun no estan integradas a Caja API.
- No se validaron escenarios de Dolares, Donacion ni pagos combinados contra Caja API.
- Permisos negativos visuales de retiro quedan pendientes.

## Recomendacion

Continuar con Fase 4F.33: validacion ampliada de lectura posterior y preparacion para integrar cierre/ventas con Caja API, manteniendo escrituras sensibles apagadas salvo la operacion especifica autorizada.
