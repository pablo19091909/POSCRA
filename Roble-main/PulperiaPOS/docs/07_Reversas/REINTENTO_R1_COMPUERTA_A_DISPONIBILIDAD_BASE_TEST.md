# Reintento R1 - Compuerta A disponibilidad base Test

Fecha UTC: 2026-07-02T01:02:44Z

## Alcance

Se ejecuto unicamente el reintento R1 de Compuerta A para Fase 5B.6A. No se autorizaron ni ejecutaron escrituras de negocio.

## Flags

Estado inicial y final validado:

- `UseCajaApiRead=true`.
- escrituras WPF apagadas.
- escrituras API apagadas.
- `EnableLegacyHashUpgrade=false`.

## Health checks

Con POS.Api iniciado temporalmente y flags de escritura apagados:

- `/health`: 200.
- `/health/database`: 200.
- `/api/system/version`: 200.

## Endpoint de reversa

Validacion segura sin escritura:

- sin token: 401.
- sin permiso `Ventas.Reversar`: 403.
- con permiso y flags apagados: 503 seguro.

## Lecturas de linea base

Lectura 1: correcta.

Resumen seguro:

- `Environment=Test` y `writes_allowed_for_testing=1`: confirmado.
- turnos abiertos para `CAJA_PRINCIPAL_TEST`: 0.
- turnos `EnCierre` para `CAJA_PRINCIPAL_TEST`: 0.
- idempotencias de venta `EnProceso`: 0.
- idempotencias de caja `EnProceso`: 0.
- reversas huerfanas de caja: 0.
- ventas: 1950.
- detalle: 5089.
- pagos: 12.
- reversas: 0.
- venta idempotencia: 12.
- venta auditoria: 12.
- movimientos de caja: 19.
- inventario agregado: 3293.00.
- saldo agregado clientes: -2957962.50.
- ingreso, retiro y cierre historicos: sin cambios observados en la lectura.

Lectura 2: bloqueada por error SQL seguro.

- Error seguro reportado: `SqlException:5`.
- No se ejecuto lectura 3 porque la regla del reintento exige detener la compuerta ante cualquier falla.

## Migracion 011

No se ejecuto migracion ni cambio de esquema durante este reintento. La validacion completa de constraints no se completo en R1 porque la compuerta se bloqueo en la segunda lectura.

## Cero escrituras

No se abrio turno, no se creo venta, no se ejecuto reversa, no se modifico inventario, no se creo movimiento de caja, no se creo idempotencia de operacion real y no se cerro caja.

## Decision

Compuerta A: bloqueada.

Motivo: una de las tres lecturas consecutivas requeridas fallo con error SQL seguro. No existe evidencia suficiente de estabilidad de base para retomar la fase manual.

## Cierre

- POS.Api detenida.
- Puerto 7046 confirmado libre.
- Flags restaurados.

## Siguiente accion

Mantener bloqueo. Reintentar Compuerta A cuando la conectividad a la base Test sea estable durante tres lecturas consecutivas.
