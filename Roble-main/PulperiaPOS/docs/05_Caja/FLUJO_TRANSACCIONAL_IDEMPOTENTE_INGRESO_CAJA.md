# Flujo transaccional idempotente Ingreso Caja

## Secuencia futura con escritura habilitada

```text
JWT y permiso Caja.Ingresar
-> Feature flag y Environment=Test
-> validar request
-> validar Idempotency-Key
-> calcular request hash
-> abrir SqlConnection
-> iniciar SqlTransaction Serializable
-> validar usuario activo
-> bloquear idempotencia por usuario + operacion + key
-> resolver estado/hash existente
-> bloquear turno Abierto de la caja
-> crear caja_idempotencia EnProceso
-> crear movimiento_caja IngresoCaja
-> marcar caja_idempotencia Completada con idMovimiento
-> commit
```

## Primera solicitud

Crea exactamente:

- un registro `caja_idempotencia`;
- un movimiento `IngresoCaja`;
- relacion con turno abierto;
- estado final `Completada`.

No inserta en `ingreso_caja` historico.

## Reintento completado

Misma key y mismo hash en estado `Completada`:

- no crea otro movimiento;
- devuelve el movimiento original;
- no expone key ni detalles internos.

## Conflictos

- Misma key con hash distinto: `409`.
- Misma key `EnProceso`: `409`.
- Estado `Fallida`: conflicto seguro hasta definir recuperacion.

## Rollback

Error antes de commit:

- rollback total;
- no debe quedar movimiento sin idempotencia completada;
- no debe quedar idempotencia completada sin movimiento;
- no se toca caja historica.
