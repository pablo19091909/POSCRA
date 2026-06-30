# Resultados de sobrante y faltante en cierre de caja

Fecha/hora UTC: 2026-06-29 23:34:01 UTC

## Turno A - Sobrante

Valores validados:

- efectivo esperado: 1000.00;
- efectivo contado: 1005.00;
- diferencia almacenada en turno: +5.00;
- movimientos totales del turno: 2;
- `FondoInicial`: 1 por 1000.00;
- `CierreDiferencia`: 1 por 5.00;
- movimientos no autorizados: 0.

Idempotencia:

- primer cierre: HTTP 200;
- repeticion con misma llave y mismo request: HTTP 200;
- conflicto con misma llave y request distinto: HTTP 409;
- idempotencias duplicadas por llave: 0.

## Turno B - Faltante

Valores validados:

- efectivo esperado: 1000.00;
- efectivo contado: 995.00;
- diferencia almacenada en turno: -5.00;
- movimientos totales del turno: 2;
- `FondoInicial`: 1 por 1000.00;
- `CierreDiferencia`: 1 por 5.00;
- movimientos no autorizados: 0.

Idempotencia:

- primer cierre: HTTP 200;
- repeticion con misma llave y mismo request: HTTP 200;
- conflicto con misma llave y request distinto: HTTP 409;
- idempotencias duplicadas por llave: 0.

## Observaciones

No se imprimieron ni documentaron llaves de idempotencia, tokens, hashes, rowVersion, usuarios, identificadores internos, connection strings ni datos personales.

No se ejecuto rollback, reapertura ni eliminacion de los turnos sinteticos creados.

