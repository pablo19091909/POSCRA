# FASE 4F.31 - Validacion visual de RetirosCajaPage con escritura Caja API apagada

Fecha UTC: 2026-06-30

## Alcance

Se ejecuto una validacion visual manual y no destructiva de `RetirosCajaPage` en `Environment=Test`.

La fase valido lectura desde Caja API para estado/resumen de turno, manteniendo el registro real de retiros en modo historico SQL. No se ejecuto ningun retiro durante esta validacion.

## Configuracion validada

- `UseCajaApiRead`: activo.
- `UseCajaApiOpenWrite`: apagado.
- `UseCajaApiIngresoWrite`: apagado.
- `UseCajaApiRetiroWrite`: apagado.
- `UseCajaApiCierreWrite`: apagado.
- `UseVentasApiWrite`: apagado.
- `EnableCajaApiWrite`: apagado.
- `EnableVentasApiWrite`: apagado.
- `EnableLegacyHashUpgrade`: apagado.

No se documentaron ni imprimieron secretos, tokens, cadenas de conexion, usuarios reales ni contrasenas.

## Linea base agregada

Antes de la validacion se confirmo:

- `caja_turno_total`: 4.
- `turno_abierto_test`: 1.
- `turno_encierre_test`: 0.
- `movimiento_caja_total`: 11.
- `caja_idempotencia_total`: 9.
- `idempotencia_enproceso`: 0.
- `ingreso_caja_total`: 9.
- `retiro_caja_total`: 6.
- `cierre_caja_total`: 15.
- `ventas_total`: 1948.
- `venta_pago_total`: 10.
- `venta_idempotencia_total`: 10.
- `inventario_stock_total`: 3296.00.
- `cliente_saldo_total`: -2957962.50.
- `precierre_efectivo_esperado`: 1100.00.

## Validacion visual en modo historico

El operador confirmo que la pantalla de retiros cargo correctamente.

Resultado observado:

- Indicador Caja API visible.
- Turno mostrado como abierto.
- Fondo inicial mostrado.
- Resumen de movimientos mostrado.
- Efectivo esperado mostrado desde lectura API.
- Texto de modo historico SQL visible para registro de retiros.
- Campos historicos de retiro visibles.
- Boton de registro visible, pero no ejecutado durante esta fase.

## Lectura API y resumen financiero

La lectura API devolvio informacion de turno abierto y resumen de caja sin habilitar escritura de retiro.

Resumen validado:

- Fondo inicial: 1000.00.
- Ingreso de caja: 100.00.
- Efectivo esperado: 1100.00.

## Confirmacion de no llamada a retiro API

Con `UseCajaApiRetiroWrite=false`, el flujo WPF mantiene el registro de retiro en modo historico SQL y no debe invocar `RegistrarRetiroAsync`.

Durante esta fase:

- No se presiono `Registrar Retiro`.
- No se envio `Idempotency-Key` para retiro.
- No se llamo al endpoint de retiro API.
- No se creo retiro API.
- No se creo movimiento de caja API.
- No se creo registro de idempotencia.

## API no disponible

Se detuvo temporalmente POS.Api y el operador valido que la pantalla maneja la API no disponible sin ejecutar escritura ni fallback destructivo.

Despues de la prueba de indisponibilidad, POS.Api fue levantada nuevamente y los health checks volvieron a responder correctamente.

## Sesion y permisos

La validacion visual fue realizada con sesion operativa autorizada.

Limitacion registrada: no se ejecuto en esta fase una prueba visual con usuario sin permiso `Caja.Ver` ni con sesion expirada. Esa validacion queda pendiente antes de habilitar escritura de retiro por API.

## Compilacion y health checks

- WPF compilo correctamente.
- POS.Api compilo correctamente.
- `/health`: HTTP 200.
- `/health/database`: HTTP 200.
- `/api/system/version`: HTTP 200.

## Integridad

La fase fue no destructiva:

- No se modifico base de datos.
- No se insertaron retiros.
- No se actualizaron movimientos.
- No se modificaron ventas.
- No se modifico inventario.
- No se modificaron saldos de clientes.
- No se ejecutaron migraciones.
- No se modifico codigo funcional.

## Riesgos pendientes

- El retiro por API permanece apagado.
- La validacion visual de permisos negativos en WPF queda pendiente.
- La pantalla muestra informacion API de lectura y campos historicos SQL al mismo tiempo; esto es correcto para la fase, pero debe revisarse UX antes de habilitar escritura API.
- La habilitacion de retiro API requiere una fase separada con idempotencia, permisos y prueba controlada.

## Recomendacion

Continuar con la Fase 4F.32: habilitacion controlada de retiro WPF por Caja API activa, manteniendo `Environment=Test`, escritura de ventas apagada, `EnableLegacyHashUpgrade=false`, prueba manual con un retiro minimo e idempotente, y validacion agregada antes/despues.
