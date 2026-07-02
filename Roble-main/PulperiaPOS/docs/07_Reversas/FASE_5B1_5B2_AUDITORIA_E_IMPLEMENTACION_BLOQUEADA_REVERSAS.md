# Fase 5B.1 + 5B.2 - Auditoria e implementacion bloqueada de reversas

Fecha UTC: 2026-07-01

## Alcance ejecutado

Se audito el modelo actual de ventas, pagos, inventario y caja para preparar reversas inmutables de ventas en efectivo integradas con Caja API. Se implemento una superficie API bloqueada por permisos y feature flags, sin ejecutar reversas reales y sin modificar datos.

## Archivos revisados

- `POS.Api/Controllers/VentasController.cs`
- `POS.Api/Application/Ventas/VentaService.cs`
- `POS.Api/Infrastructure/Data/Ventas/VentaRepository.cs`
- `POS.Api/Application/Caja/CajaService.cs`
- `POS.Api/Infrastructure/Data/Caja/CajaRepository.cs`
- `POS.Api/Domain/PermissionNames.cs`
- `POS.Api/Infrastructure/Security/RolePermissionProvider.cs`
- `PulperiaPOS/Views/VentasCrudWindow.xaml.cs`
- `PulperiaPOS/VentasPage.xaml.cs`
- `database/migrations/007_SoporteVentasTransaccionales.sql`
- `database/migrations/008_CajaTurnosYMovimientos.sql`
- `database/migrations/009_IdempotenciaCajaApi.sql`
- `database/migrations/010_CajaIdempotenciaAbrirTurno.sql`

## Implementacion bloqueada

Se agregaron contratos y servicio de reversa para `POST /api/ventas/{factura}/reversas`. El endpoint exige autenticacion JWT y permiso `Ventas.Reversar`.

La ejecucion queda bloqueada si cualquiera de estas compuertas esta apagada:

- `EnableVentasApiWrite`
- `EnableCajaApiWrite`
- `EnableVentasApiReversaCajaWrite`

Aunque las compuertas se prendieran, la fase deja la ejecucion real como no disponible hasta que exista una migracion especifica de soporte de reversas de venta.

## Resultados tecnicos

- Build solucion completa: correcto, cero errores, advertencias heredadas.
- Build POS.Api: correcto, cero errores.
- Health API: HTTP 200.
- Health database: HTTP 200.
- Version API: HTTP 200.
- Endpoint sin token: HTTP 401.
- Endpoint con token sin permiso: HTTP 403.
- Endpoint con permiso y flags apagados: HTTP 503 seguro.

## Integridad de datos

Se tomaron agregados antes y despues con consultas `SELECT`. Los valores permanecieron iguales:

- ventas: sin cambios.
- detalle de venta: sin cambios.
- pagos de venta: sin cambios.
- idempotencia de venta: sin cambios.
- auditoria de venta: sin cambios.
- inventario y stock agregado: sin cambios.
- clientes y saldo agregado: sin cambios.
- turnos de caja: sin cambios.
- movimientos de caja: sin cambios.
- idempotencia de caja: sin cambios.
- tablas historicas de ingreso, retiro y cierre: sin cambios.

## Confirmaciones

- No se ejecutaron reversas reales.
- No se ejecutaron migraciones.
- No se ejecutaron escrituras SQL contra datos de negocio.
- No se modifico WPF funcional.
- No se modifico `DBConnection.cs`.
- No se modifico `CajaHelper`.
- No se tocaron Cliente General ni ventas historicas.
- No se imprimieron secretos, tokens ni cadenas de conexion.
- POS.Api fue detenida al finalizar las validaciones.

## Riesgos pendientes

- Falta migracion formal para registrar reversas de venta e idempotencia especifica.
- Falta repositorio transaccional real de reversa.
- Falta prueba controlada con una venta elegible en Test.
- El WPF aun conserva borrado fisico historico en `VentasCrudWindow`.
- Falta integrar UI de reversa, manteniendola apagada por flag.

## Recomendacion

Continuar con Fase 5B.3: diseno y migracion no destructiva de soporte persistente para reversas inmutables de venta en efectivo, sin habilitar ejecucion real todavia.
