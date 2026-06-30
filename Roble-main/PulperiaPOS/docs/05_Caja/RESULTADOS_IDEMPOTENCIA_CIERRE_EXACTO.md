# Resultados de idempotencia - cierre exacto de turno

Fecha/hora UTC: 2026-06-29 23:15:15 UTC

## Escenario probado

Operacion: `CerrarTurno`.

Ambiente: `Environment=Test`.

Caja: `CAJA_PRINCIPAL_TEST`.

Efectivo contado usado para el cierre exacto: 201.00.

No se revelan llaves de idempotencia, tokens, usuarios, identificadores internos ni rowVersion.

## Resultados HTTP

- Primer cierre con llave nueva y solicitud valida: HTTP 200.
- Repeticion con la misma llave y el mismo cuerpo: HTTP 200.
- Misma llave con cuerpo diferente: HTTP 409.
- Nueva llave contra turno ya cerrado: HTTP 409.

## Resultado persistente

- `caja_idempotencia` total posterior: 5.
- Idempotencias `IngresoCaja` completadas: 2.
- Idempotencias `RetiroCaja` completadas: 2.
- Idempotencias `CerrarTurno` totales: 1.
- Idempotencias `CerrarTurno` completadas: 1.
- Idempotencias `CerrarTurno` en proceso: 0.
- Idempotencias `CerrarTurno` fallidas: 0.
- Idempotencias totales en proceso: 0.
- Idempotencias totales fallidas: 0.
- Idempotencia de cierre sin turno asociado: 0.
- Llaves duplicadas por usuario, operacion y llave: 0.

## Interpretacion

La operacion de cierre se comporto como idempotente:

- el primer intento cerro el turno;
- el reintento identico devolvio el mismo resultado funcional;
- una solicitud distinta con la misma llave fue rechazada;
- no se permitio crear otro cierre con una llave distinta una vez cerrado el turno.

## Resultado

Validacion aprobada.

