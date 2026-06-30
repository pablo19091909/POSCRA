# Fase 4F.23 - Auditoria e integracion base WPF Caja API bloqueada

Fecha/hora UTC: 2026-06-30 02:13:13 UTC

## Alcance

Se preparo la base de integracion WPF con Caja API sin activar escrituras y sin sustituir los flujos historicos SQL.

La fase agrego:

- flags WPF independientes para caja;
- contratos WPF de lectura de Caja API;
- cliente WPF de lectura para Caja API;
- mapeador de errores seguros;
- coordinador reusable para futuras operaciones idempotentes;
- documentacion de arquitectura y migracion gradual.

No se conectaron botones de apertura, ingreso, retiro ni cierre a endpoints de escritura API.

## Auditoria del flujo WPF historico

Paginas revisadas:

- `IngresoCajaPage.xaml` y `IngresoCajaPage.xaml.cs`;
- `Views/RetirosCajaPage.xaml` y `Views/RetirosCajaPage.xaml.cs`;
- `Views/CierreCajaPage.xaml` y `Views/CierreCajaPage.xaml.cs`;
- `CajaHelper.cs`;
- ventanas principales de navegacion;
- `UserSession`;
- `ApiClientBase` y clientes API existentes;
- `FeatureFlags`.

Operacion historica por pagina:

- `IngresoCajaPage`: inserta en `ingreso_caja`, recarga historial y recalcula caja desde SQL.
- `RetirosCajaPage`: valida dinero disponible desde `CajaHelper`, inserta en `retiro_caja`, imprime recibo y recarga historial.
- `CierreCajaPage`: calcula totales desde ventas/caja historica, inserta en `cierre_caja`, imprime cierre y recarga historial.
- `CajaHelper`: calcula efectivo con `ventas`, `ingreso_caja`, `retiro_caja`, `cierre_caja` y saldo de clientes.

Dependencias actuales:

- cierre historico depende de ventas del dia, ingresos historicos, retiros historicos y cierres previos;
- retiro historico depende del calculo acumulado de caja;
- ingreso historico no depende de turno abierto;
- no existe idempotencia ni proteccion local contra doble clic en los botones historicos.

## Cambios realizados

Archivos modificados:

- `PulperiaPOS/Configuration/FeatureFlags.cs`;
- `PulperiaPOS/appsettings.json`;
- `PulperiaPOS/appsettings.Development.json.example`.

Archivos creados:

- `PulperiaPOS/ApiClients/CajaApiClient.cs`;
- `PulperiaPOS/ApiClients/CajaApiErrorMapper.cs`;
- `PulperiaPOS/Models/Caja/CajaTurnoApiResponse.cs`;
- `PulperiaPOS/Models/Caja/MovimientoCajaApiResponse.cs`;
- `PulperiaPOS/Models/Caja/PreCierreCajaApiResponse.cs`;
- `PulperiaPOS/Models/Caja/ResumenMovimientoCajaApiResponse.cs`;
- `PulperiaPOS/Models/Caja/CajaOperationCoordinator.cs`;
- `PulperiaPOS/Models/Caja/PendingCajaOperation.cs`;
- `PulperiaPOS/Models/Caja/CajaOperationState.cs`.

No se modificaron:

- `IngresoCajaPage.xaml`;
- `IngresoCajaPage.xaml.cs`;
- `RetirosCajaPage.xaml`;
- `RetirosCajaPage.xaml.cs`;
- `CierreCajaPage.xaml`;
- `CierreCajaPage.xaml.cs`;
- `CajaHelper.cs`;
- `VentasPage`;
- `DBConnection.cs`.

## Pruebas no destructivas

Linea base antes y despues:

- `caja_turno`: 3;
- `movimiento_caja`: 9;
- `CierreDiferencia`: 2;
- `idempotencias CerrarTurno Completada`: 3;
- turnos abiertos: 0;
- turnos `EnCierre`: 0;
- turnos cerrados Test: 3;
- idempotencias `EnProceso`: 0;
- idempotencias `Fallida`: 0.

Tablas y agregados sin cambios:

- `ingreso_caja`: 9;
- `retiro_caja`: 6;
- `cierre_caja`: 15;
- `ventas`: 1948;
- `venta_pago`: 10;
- `venta_idempotencia`: 10;
- inventario agregado: 3296.00;
- saldo agregado de clientes: -2957962.50.

Lecturas API ejecutadas con escrituras apagadas:

- `/health`: HTTP 200;
- `/health/database`: HTTP 200;
- `/api/system/version`: HTTP 200;
- `GET /api/caja/turnos/abierto` sin token: HTTP 401;
- `GET /api/caja/turnos/abierto` sin permiso: HTTP 403;
- `GET /api/caja/turnos/abierto` autorizado: HTTP 204 por no existir turno abierto;
- API detenida: error de red seguro.

## Resultado

Fase aprobada con escrituras bloqueadas.

No se crearon turnos, movimientos, cierres, idempotencias ni cambios historicos.

