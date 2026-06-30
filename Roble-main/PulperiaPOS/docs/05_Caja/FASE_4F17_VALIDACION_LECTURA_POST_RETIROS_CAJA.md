# Fase 4F.17 - Validacion de lectura post retiros Caja API

Fecha UTC: 2026-06-29 21:02:52 UTC

## Alcance

Se ejecuto una validacion operativa de solo lectura posterior a los retiros idempotentes de Caja API en base marcada como `Environment=Test`.

No se activaron feature flags de escritura y no se ejecutaron operaciones `POST`, migraciones ni scripts de escritura.

## Flags

- `UseVentasApiWrite=false`.
- `EnableVentasApiWrite=false`.
- `EnableCajaApiWrite=false`.

## Seguridad de rutas de lectura

Rutas validadas:

- `GET /api/caja/turnos/abierto?cajaCodigo=CAJA_PRINCIPAL_TEST`.
- `GET /api/caja/turnos/{id}/movimientos`.
- `GET /api/caja/turnos/{id}/pre-cierre`.

Resultados:

- sin token: `401`;
- token sin `Caja.Ver`: `403`;
- token autorizado con `Caja.Ver`: `200`;
- no se expusieron tokens, hashes, keys, SQL, host, puerto ni datos personales en la evidencia.

## Turno abierto

- Existe exactamente un turno `Abierto` para `CAJA_PRINCIPAL_TEST`.
- No existe segundo turno `Abierto` o `EnCierre` para esa caja.
- Fondo inicial: `1000.00`.
- Campos de cierre: `NULL`.
- Usuario de apertura valido: confirmado por agregado.
- `row_version` no cambio tras las lecturas.
- No existen cierres API.

## Resumen de movimientos

- `FondoInicial`: `1`, total `1000.00`.
- `IngresoCaja`: `2`, total `501.00`.
- `RetiroCaja`: `2`, total `1300.00`.
- `VentaEfectivo`: `0`.
- `AjustePositivo`: `0`.
- `AjusteNegativo`: `0`.
- `DevolucionEfectivo`: `0`.
- `CierreDiferencia`: `0`.
- `Reversa`: `0`.

Integridad:

- Total de movimientos del turno: `5`.
- Todos tienen monto positivo.
- Todos tienen moneda valida.
- Todos tienen fecha UTC.
- No hay movimientos huerfanos.
- No hay relaciones con factura, pago, ingreso historico, retiro historico o reversa.
- No hay duplicidad por repeticion de idempotencia.

## Idempotencia

- Idempotencias Caja API totales: `4`.
- Todas estan `Completada`.
- `IngresoCaja`: `2`.
- `RetiroCaja`: `2`.
- `EnProceso`: `0`.
- `Fallida`: `0`.
- Cada idempotencia completada se relaciona con un movimiento.
- Cada movimiento de ingreso/retiro tiene idempotencia completada.
- No hay duplicidad por usuario + operacion + key.
- No existen multiples idempotencias completadas para el mismo movimiento.
- Los intentos insuficientes y conflictos no dejaron idempotencias completadas ni movimientos.

## Pre-cierre

El pre-cierre respondio `200` y calcula desde `movimiento_caja`:

- `FondoInicial`: `1000.00`.
- `IngresosCaja`: `501.00`.
- `RetirosCaja`: `1300.00`.
- `VentasEfectivo`: `0.00`.
- `Ajustes`: `0.00`.
- `Devoluciones`: `0.00`.
- `DiferenciaCierre`: `0.00`.
- `EfectivoEsperado`: `201.00`.

No escribio datos ni altero turno o movimientos.

## Aislamiento historico

Linea base antes y despues sin cambios:

- `caja_turno=1`.
- `movimiento_caja=5`.
- `caja_idempotencia=4`.
- `ingreso_caja=9`.
- `retiro_caja=6`.
- `cierre_caja=15`.
- `ventas=1948`.
- `venta_pago=10`.
- `venta_idempotencia=10`.
- inventario agregado `3296.00`.
- saldo agregado clientes `-2957962.50`.
- `Cliente General`: `1`.

No se detecto actividad externa concurrente durante la validacion.

## Riesgos pendientes

- Cierre API aun no implementado.
- Falta transicion transaccional `Abierto` a `EnCierre`.
- Falta idempotencia para `CerrarTurno`.
- Falta bloqueo de ingresos, retiros y ventas en efectivo durante `EnCierre` o `Cerrado`.
- Falta integracion futura de ventas en efectivo con `MovimientoCaja`.
- WPF aun no usa Caja API para caja operativa.

## Recomendacion

Continuar con Fase 4F.18: diseno tecnico y preparacion no ejecutada del cierre de turno API, sin aplicar escrituras todavia.
