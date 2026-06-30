# Contrato Idempotency-Key para retiros

Fecha UTC: 2026-06-29 16:17:55 UTC

## Endpoint

```text
POST /api/caja/retiros
```

## Header

```text
Idempotency-Key: GUID
```

Cuando `EnableCajaApiWrite=false`, el endpoint responde `503` antes de validar este header.

Cuando la escritura este habilitada:

- key ausente o invalida: `400`;
- misma key y mismo request: resultado repetido;
- misma key y request distinto: `409`;
- key en proceso: `409`.

## Body permitido

```text
cajaCodigo
monto
motivo
referencia
```

## Body no permitido

El cliente no envia:

- usuario;
- idTurno;
- fecha;
- estado;
- efectivo disponible;
- hash;
- idMovimiento;
- informacion de cierre.

## Hash canonico

Campos incluidos:

- operacion fija `RetiroCaja`;
- usuario autenticado;
- caja normalizada;
- monto con dos decimales y cultura invariante;
- motivo normalizado;
- referencia normalizada.

Campos excluidos:

- timestamps;
- token;
- efectivo disponible;
- ids generados;
- datos derivados.

El hash no se imprime ni se expone.
