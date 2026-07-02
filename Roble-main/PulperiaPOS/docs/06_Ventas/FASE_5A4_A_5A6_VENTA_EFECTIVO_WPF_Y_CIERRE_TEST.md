# Fase 5A.4 a 5A.6 - Venta efectivo WPF y cierre exacto Test

Fecha UTC: 2026-07-01.

## Alcance

Se ejecuto la integracion WPF de venta en efectivo por POS.Api y Caja API, seguida del cierre exacto del turno Test.

Ambiente autorizado:

- `Environment=Test`.
- `writes_allowed_for_testing=1`.
- Caja logica Test.

No se usaron scripts SQL de escritura, migraciones, Postman ni inserciones manuales para crear la venta o cerrar el turno.

## Auditoria e integracion

Archivos revisados:

- `PulperiaPOS/VentasPage.xaml`.
- `PulperiaPOS/VentasPage.xaml.cs`.
- `PulperiaPOS/ApiClients/VentasApiClient.cs`.
- `PulperiaPOS/ApiClients/CajaApiClient.cs`.
- `PulperiaPOS/ApiClients/ApiClientBase.cs`.
- `PulperiaPOS/Models/Ventas/*`.
- `PulperiaPOS/UserSession.cs`.
- `PulperiaPOS/Configuration/FeatureFlags.cs`.
- `PulperiaPOS/Views/CierreCajaPage.xaml.cs`.

Cambios realizados:

- `VentasPage` muestra `Modo Venta API` cuando `UseVentasApiEfectivoWrite=true`.
- Pago `Efectivo` usa POS.Api exclusivamente cuando `UseVentasApiEfectivoWrite=true`.
- La ruta efectiva valida turno abierto y pre-cierre por Caja API antes de confirmar.
- La ruta efectiva no llama `DBConnection`, `CajaHelper` ni `RawPrinterHelper` para persistir.
- La impresion historica queda omitida para venta efectiva API.
- Se corrigio doble clic: la operacion se marca en proceso y el boton se bloquea desde el primer clic.

## Compuerta previa

Con escrituras del servidor apagadas:

- `/health`: 200.
- `/health/database`: 200.
- `/api/system/version`: 200.
- Sin token: 401.
- Token sin permiso: 403.
- Token autorizado con flags apagados: 503 seguro.
- No se creo venta.
- No se desconto inventario.
- No se creo movimiento `VentaEfectivo`.

Turno previo:

- Turno abierto: 1.
- Turno en cierre: 0.
- Fondo inicial: 1000.00.
- Venta efectivo API previa: 10.00.
- Efectivo esperado previo a venta WPF: 1010.00.

## Venta WPF

El operador ejecuto una unica venta desde `VentasPage` WPF.

Resultado:

- Venta WPF: exitosa.
- Metodo: efectivo.
- La UI mostro confirmacion de impacto en Caja API.
- El carrito se limpio solo despues del exito confirmado.
- Se detecto doble mensaje al intentar doble clic; se corrigio en codigo antes del cierre.

Variacion validada posterior a venta WPF:

- `ventas`: +1 respecto a la linea base de esta fase.
- `DetalleVenta`: +1.
- `venta_pago`: +1.
- `venta_idempotencia`: +1.
- `venta_auditoria`: +1.
- `movimiento_caja`: +1 `VentaEfectivo`.
- No hubo ingresos, retiros, ajustes, reversas ni `CierreDiferencia`.
- No hubo cambios en tablas historicas.

Pre-cierre despues de venta WPF:

- Fondo inicial: 1000.00.
- `VentaEfectivo`: 2 movimientos.
- Total `VentaEfectivo`: 1510.00.
- Efectivo esperado: 2510.00.

## Cierre exacto

El operador ejecuto cierre desde `CierreCajaPage` WPF con Caja API.

Resultado final:

- Turno abierto: 0.
- Turno en cierre: 0.
- Turno cerrado: confirmado.
- Efectivo esperado: 2510.00.
- Efectivo contado: 2510.00.
- Diferencia: 0.00.
- `CierreDiferencia`: 0.
- Idempotencia `CerrarTurno` completada: 1.

## Restauracion

Flags finales:

- `UseCajaApiRead=true` en configuracion local WPF.
- `UseCajaApiOpenWrite=false`.
- `UseCajaApiIngresoWrite=false`.
- `UseCajaApiRetiroWrite=false`.
- `UseCajaApiCierreWrite=false`.
- `UseVentasApiWrite=false`.
- `UseVentasApiEfectivoWrite=false`.
- `EnableVentasApiWrite=false`.
- `EnableCajaApiWrite=false`.
- `EnableVentasApiEfectivoCajaWrite=false`.
- `EnableLegacyHashUpgrade=false`.

POS.Api fue detenida y el puerto local quedo libre.

## Limitaciones pendientes

- Reversas inmutables no implementadas.
- Tarjeta, SINPE, saldo, dolares, donacion y pagos combinados fuera de alcance.
- Produccion no autorizada.
- Corte formal de flujos historicos pendiente.

## Recomendacion

Continuar con el siguiente modulo de corte gradual, manteniendo ventas historicas disponibles por flag y preparando el endurecimiento de consulta/reporteria de ventas y caja.
