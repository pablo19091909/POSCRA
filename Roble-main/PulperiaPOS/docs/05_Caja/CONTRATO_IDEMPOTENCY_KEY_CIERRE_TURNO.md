# Contrato Idempotency-Key cierre turno

Fecha UTC: 2026-06-29 21:37:22 UTC

## Header

Cuando escritura este habilitada:

```text
Idempotency-Key: <GUID>
```

Con flag apagado, POS.Api responde `503` antes de validar la key.

## Operacion

```text
CerrarTurno
```

## Hash canonico implementado

Incluye:

- operacion fija `CerrarTurno`;
- usuario autenticado;
- id logico del turno;
- caja normalizada;
- efectivo contado con dos decimales y cultura invariante;
- observacion normalizada;
- `rowVersion` normalizada.

No incluye:

- fecha/hora;
- token;
- efectivo esperado;
- diferencia;
- SQL;
- request completo.

## Pruebas puras ejecutadas

- mismo request produce hash estable: correcto;
- cambio de contado cambia hash: correcto;
- cambio de observacion cambia hash: correcto;
- cambio de rowVersion cambia hash: correcto.

## Estados de idempotencia

- `EnProceso`: conflicto seguro.
- `Completada` con mismo hash: devuelve resultado seguro previo.
- `Completada` con hash distinto: conflicto.
- `Fallida`: no se reutiliza silenciosamente.

## Referencia de cierre

La idempotencia de cierre se completa con `cierre_referencia_id` apuntando al turno cerrado. Si existe `CierreDiferencia`, tambien puede registrar `idMovimiento`.
