# Fase 4F.12 - Prueba de ingreso idempotente Caja API Test

Fecha UTC: 2026-06-29 15:07:18 UTC

## Alcance ejecutado

Se ejecuto una prueba controlada en la base marcada como `Environment=Test`, con escrituras de prueba habilitadas a nivel de ambiente y `EnableCajaApiWrite` activado solo como variable temporal del proceso de POS.Api.

La prueba creo exclusivamente:

- un movimiento `IngresoCaja` de `500.00`;
- una idempotencia `Completada` asociada;
- una prueba de concurrencia real autorizada por `1.00`, con una segunda idempotencia `Completada`.

No se crearon retiros, cierres, ajustes, reversas, ventas, pagos, clientes, movimientos historicos ni registros en tablas historicas.

## Revision previa

`POS.Api/POS.Api.csproj` fue revisado. El cambio vigente solo evita copiar archivos locales de entorno (`appsettings.Development.json`, `appsettings.Test.json`, `appsettings.Production.json`) a salida o publicacion.

Confirmaciones:

- no bloquea la lectura de configuracion local desde el directorio del proyecto;
- no altera `appsettings.json`;
- no activa `EnableCajaApiWrite` por defecto;
- no activa `EnableVentasApiWrite`;
- no modifica WPF;
- no copia `appsettings.Development.json` a `POS.Api/bin` ni `POS.Api/obj`.

## Linea base previa

Resultados agregados antes de la prueba:

- `Environment=Test` con escrituras de prueba permitidas: confirmado.
- Turno abierto `CAJA_PRINCIPAL_TEST`: `1`.
- Movimiento `FondoInicial` de `1000.00`: `1`.
- Movimientos `IngresoCaja`: `0`.
- `caja_idempotencia`: `0`.
- Efectivo esperado: `1000.00`.
- Usuarios activos autorizados por rol para Caja API: existe al menos uno.
- `caja_turno`: `1`.
- `movimiento_caja`: `1`.
- `ingreso_caja`: `9`.
- `retiro_caja`: `6`.
- `cierre_caja`: `15`.
- `ventas`: `1948`.
- `venta_pago`: `10`.
- Inventario agregado: `3296.00`.
- Saldo agregado de clientes: `-2957962.50`.

## Activacion temporal

Se activo solo en el proceso local de POS.Api:

- `FeatureFlags__EnableCajaApiWrite=true`.
- `FeatureFlags__EnableVentasApiWrite=false`.
- `EnvironmentSafety__RequiredDatabaseEnvironment=Test`.
- `EnvironmentSafety__BlockWritesUnlessDatabaseEnvironmentMatches=true`.

No se modifico ningun archivo versionado para activar el flag.

Health checks con API activa:

- `/health`: `200`.
- `/health/database`: `200`.
- `/api/system/version`: `200`.

## Solicitudes ejecutadas

Solicitud principal:

- endpoint: `POST /api/caja/ingresos`;
- caja: `CAJA_PRINCIPAL_TEST`;
- monto: `500.00`;
- motivo: `Ingreso sintetico controlado Fase 4F.12`;
- referencia: `TEST-INGRESO-F4F12`;
- resultado HTTP: `200`;
- respuesta segura: tipo `IngresoCaja`, monto `500.00`, turno presente, fecha UTC presente.

Repeticion con la misma key y mismo request:

- resultado HTTP: `200`;
- retorno equivalente;
- no duplico movimiento;
- no duplico idempotencia.

Misma key con request distinto:

- cambio controlado de intencion;
- resultado HTTP: `409`;
- respuesta segura con error funcional;
- no duplico movimiento;
- no modifico historicos.

Concurrencia real:

- se uso una key nueva;
- se enviaron dos solicitudes HTTP simultaneas con el mismo body;
- monto autorizado: `1.00`;
- resultados HTTP: `200` y `200`;
- resultado final: una sola intencion concurrente persistida, sin duplicacion por key.

No se documentan ni conservan valores de token, usuario, hash ni idempotency key.

## Resultado posterior

Resultados agregados despues de la prueba:

- Turno abierto `CAJA_PRINCIPAL_TEST`: `1`.
- Movimiento `FondoInicial` de `1000.00`: `1`.
- Movimientos `IngresoCaja`: `2`.
- Movimiento `IngresoCaja` de `500.00`: `1`.
- Movimiento `IngresoCaja` de `1.00`: `1`.
- Otros movimientos API de caja: `0`.
- `caja_idempotencia`: `2`.
- Idempotencias `Completada`: `2`.
- Idempotencias `EnProceso`: `0`.
- Idempotencias `Fallida`: `0`.
- Duplicados por key/usuario/operacion: `0`.
- Idempotencias completadas sin movimiento: `0`.
- Movimientos `IngresoCaja` sin idempotencia completada: `0`.
- Efectivo esperado: `1501.00`.

## Integridad historica

Sin cambios en:

- `ingreso_caja`: permanece `9`;
- `retiro_caja`: permanece `6`;
- `cierre_caja`: permanece `15`;
- `ventas`: permanece `1948`;
- `venta_pago`: permanece `10`;
- inventario agregado: permanece `3296.00`;
- saldo agregado de clientes: permanece `-2957962.50`.

## Restauracion

Estado final:

- `EnableCajaApiWrite=false` en configuracion versionada/local de Test.
- `EnableVentasApiWrite=false`.
- `UseVentasApiWrite=false`.
- POS.Api detenida.
- Puerto `7046` libre.

Los dos movimientos de ingreso y sus dos idempotencias permanecen como evidencia Test para fases futuras.

## Limitaciones vigentes

- WPF Caja API aun no esta integrado.
- Retiro API fuera de alcance.
- Cierre API fuera de alcance.
- Idempotencia persistente en apertura de caja fuera de alcance.
- Integracion de ventas API con movimiento de efectivo fuera de alcance.
- Anulacion o reversa fuera de alcance.
- Dolares, donacion y pagos combinados fuera de alcance.

## Recomendacion

Continuar con Fase 4F.13: validar lectura operativa posterior desde API y preparar la integracion gradual de WPF para consultar el turno y pre-cierre sin habilitar escrituras de caja desde WPF.
