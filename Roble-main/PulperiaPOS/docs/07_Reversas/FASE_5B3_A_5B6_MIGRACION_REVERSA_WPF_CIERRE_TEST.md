# Fase 5B.3 a 5B.6 - Migracion, integracion bloqueada y corte de compuerta

Fecha UTC: 2026-07-01

## Estado

La fase fue ejecutada hasta la Compuerta C. Se detuvo antes del Bloque D porque las siguientes operaciones deben originarse exclusivamente desde WPF real:

- apertura de turno;
- venta en efectivo;
- reversa;
- cierre exacto.

No se sustituyeron esas acciones por llamadas directas, SQL manual, scripts de insercion ni herramientas externas.

## Linea base inicial

- `Environment=Test`: confirmado.
- `writes_allowed_for_testing=1`: confirmado.
- turnos abiertos para `CAJA_PRINCIPAL_TEST`: 0.
- turnos `EnCierre` para `CAJA_PRINCIPAL_TEST`: 0.
- idempotencias de venta `EnProceso`: 0.
- idempotencias de caja `EnProceso`: 0.
- reversas de caja huerfanas: 0.
- ventas: 1950.
- detalle de venta: 5089.
- pagos: 12.
- movimientos de caja: 19.
- inventario agregado: 3293.00.
- saldo agregado clientes: -2957962.50.

## Migracion aplicada

Se aplico `database/migrations/011_ReversaVentaEfectivoInmutable.sql`.

La migracion creo `dbo.venta_reversa` y ajusto el constraint de eventos de `dbo.venta_auditoria` para permitir `VentaReversada`.

No hizo backfill y no altero datos de negocio.

## Integracion bloqueada

Se agrego el endpoint transaccional real `POST /api/ventas/{factura}/reversas`, protegido por:

- JWT;
- permiso `Ventas.Reversar`;
- `EnableVentasApiWrite`;
- `EnableCajaApiWrite`;
- `EnableVentasApiReversaCajaWrite`;
- `Environment=Test`.

Se agrego en WPF un flujo minimo en `VentasCrudWindow` con `Modo Reversa API`, razon obligatoria y confirmacion irreversible, detras de `UseVentasApiReversaWrite`.

## Validacion de seguridad

Con flags apagados:

- `/health`: 200.
- `/health/database`: 200.
- `/api/system/version`: 200.
- reversa sin token: 401.
- reversa sin permiso: 403.
- reversa con permiso y flags apagados: 503 seguro.

## Corte de compuerta

No se abrio turno, no se creo venta, no se ejecuto reversa real y no se cerro turno, porque esas acciones requieren operacion manual desde WPF para cumplir el prompt.

## Linea base final

Los conteos de negocio permanecieron iguales. La unica variacion esperada fue `venta_reversa_table_exists=1`.

## Flags finales

Restaurados:

- `UseCajaApiRead=true`.
- todas las escrituras WPF apagadas.
- todas las escrituras API apagadas.
- `EnableLegacyHashUpgrade=false`.

## Recomendacion

Ejecutar una fase manual guiada 5B.6A para abrir turno, vender, reversar y cerrar desde WPF, usando las compuertas ya implementadas.
