# Validacion de bloqueo de operaciones sobre turno cerrado

Fecha/hora UTC: 2026-06-29 23:15:15 UTC

## Objetivo

Confirmar que, una vez cerrado el turno `CAJA_PRINCIPAL_TEST`, POS.Api no permite registrar operaciones nuevas sobre ese turno.

## Pruebas ejecutadas

Despues del cierre exacto:

- `POST /api/caja/turnos/{id}/cerrar` con llave nueva: HTTP 409.
- `POST /api/caja/ingresos`: HTTP 409.
- `POST /api/caja/retiros`: HTTP 409.

Las respuestas fueron seguras y no expusieron secretos, identificadores internos ni datos de configuracion.

## Validacion de datos posterior

- Turno abierto: 0.
- Turno `EnCierre`: 0.
- Turno cerrado: 1.
- Movimientos totales del turno: 5.
- Movimientos `IngresoCaja`: 2 por 501.00.
- Movimientos `RetiroCaja`: 2 por 1300.00.
- Movimientos `CierreDiferencia`: 0.
- Reversas: 0.
- Movimientos huerfanos: 0.

## Resultado

Bloqueo aprobado.

No se crearon ingresos, retiros, cierres adicionales, diferencias, ajustes, reversas ni movimientos posteriores al cierre.

