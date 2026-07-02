# Fase 5B.6A - Prueba manual reversa WPF y cierre Test

Fecha UTC: 2026-07-02T01:46:29Z

## Alcance ejecutado

Se retomo la fase desde Bloque B, sin repetir migraciones, auditoria de esquema, diseno ni diagnostico R2.

La prueba se ejecuto en `Environment=Test` con `writes_allowed_for_testing=1`, usando `https://localhost:7046`.

Secuencia ejecutada:

1. Apertura WPF de turno Test.
2. Venta efectiva WPF por API.
3. Reversa total inmutable WPF por API.
4. Cierre exacto WPF por API.

## Estabilidad previa

R2 habia validado cinco ciclos consecutivos exitosos:

- Apertura SQL.
- `SELECT 1`.
- Marca `Environment=Test`.
- Lectura agregada de caja, idempotencia y reversas.

Antes de iniciar esta fase:

- `/health`: HTTP 200.
- `/health/database`: HTTP 200.
- `/api/system/version`: HTTP 200.
- Turnos abiertos Test: 0.
- Turnos EnCierre Test: 0.
- Idempotencias pendientes: 0.
- Reversas huerfanas: 0.

## Flags por compuerta

Inicialmente:

- `UseCajaApiRead=true`.
- Todas las flags WPF de escritura estaban en `false`.
- Todas las flags API de escritura estaban en `false`.
- `EnableLegacyHashUpgrade=false`.

Durante apertura:

- `UseCajaApiOpenWrite=true`.
- `EnableCajaApiWrite=true`.

Durante venta:

- `UseVentasApiEfectivoWrite=true`.
- `EnableVentasApiWrite=true`.
- `EnableCajaApiWrite=true`.
- `EnableVentasApiEfectivoCajaWrite=true`.

Durante reversa:

- `UseVentasApiReversaWrite=true`.
- `EnableVentasApiWrite=true`.
- `EnableCajaApiWrite=true`.
- `EnableVentasApiReversaCajaWrite=true`.

Durante cierre:

- `UseCajaApiCierreWrite=true`.
- `EnableCajaApiWrite=true`.

Al final se restauraron las flags seguras:

- `UseCajaApiRead=true`.
- Escrituras WPF apagadas.
- Escrituras API apagadas.
- `EnableLegacyHashUpgrade=false`.

## Apertura

Accion manual confirmada por operador:

- Caja: `CAJA_PRINCIPAL_TEST`.
- Fondo inicial: `1000.00`.
- Observacion: `Turno Test para reversa inmutable VentaEfectivo`.

Resultado validado:

- Turno abierto Test: +1.
- Turno EnCierre: 0.
- Movimiento `FondoInicial`: +1.
- Idempotencia `AbrirTurno`: +1 `Completada`.
- Ventas nuevas en esta compuerta: 0.
- Reversas nuevas en esta compuerta: 0.

## Venta efectiva

Accion manual confirmada por operador:

- Modo Venta API visible.
- Producto: `API_TEST_PROD_STOCK_ALTO`.
- Cantidad: 1.
- Metodo: Efectivo.
- Total: `10.00`.
- Factura de prueba: `2002`.

Resultado validado:

- `ventas`: +1.
- `DetalleVenta`: +1.
- `venta_pago`: +1.
- `venta_idempotencia`: +1 `Completada`.
- `venta_auditoria`: +1.
- `movimiento_caja` tipo `VentaEfectivo`: +1.
- Stock de producto: 90 -> 89.
- No se creo reversa en esta compuerta.

## Incidencia corregida durante reversa

Al abrir `VentasCrudWindow` aparecio una excepcion XAML por texto literal `` `r`n `` dentro del `StackPanel` de acciones.

Correccion indispensable aplicada:

- Archivo: `PulperiaPOS/Views/VentasCrudWindow.xaml`.
- Cambio: remover literales invalidos y dejar solo botones WPF reales dentro del `StackPanel`.
- No se modifico logica de negocio.
- No se modifico base de datos.

Compilacion WPF posterior: 0 errores.

## Reversa

Accion manual confirmada por operador:

- Modo Reversa API disponible.
- Venta seleccionada: factura `2002`.
- Motivo: `Reversa total sintetica controlada de VentaEfectivo Test`.

Resultado validado:

- Venta original sigue existiendo.
- `venta_reversa`: +1.
- `movimiento_caja` tipo `Reversa`: +1.
- `venta_idempotencia`: +1 `Completada`.
- `venta_auditoria`: +1.
- Reversas para la venta: 1.
- Reversas huerfanas: 0.
- Stock de producto: 89 -> 90.
- Efecto neto de inventario: 0.

## Cierre exacto

Accion manual confirmada por operador:

- Modo Caja API visible.
- Efectivo contado: `1000.00`.
- Observacion: `Cierre exacto Test posterior a reversa inmutable VentaEfectivo`.

Resultado validado:

- Estado final del turno: `Cerrado`.
- Efectivo esperado: `1000.00`.
- Efectivo contado: `1000.00`.
- Diferencia: `0.00`.
- Turno abierto Test: 0.
- Turno EnCierre Test: 0.
- Idempotencia `CerrarTurno`: +1 `Completada`.
- No se creo movimiento `CierreDiferencia` nuevo.
- No se modifico `cierre_caja`.

## Resultado final

Secuencia completa aprobada:

- Apertura WPF por Caja API.
- Venta efectiva WPF por Ventas API.
- Reversa total inmutable WPF por API.
- Cierre exacto WPF por Caja API.

