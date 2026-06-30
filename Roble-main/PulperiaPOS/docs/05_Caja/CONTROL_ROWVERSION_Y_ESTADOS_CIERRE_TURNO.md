# Control rowVersion y estados cierre turno

Fecha UTC: 2026-06-29 21:37:22 UTC

## rowVersion

El request de cierre requiere `rowVersion` en Base64. POS.Api lo valida como arreglo de 8 bytes y lo compara dentro de la transaccion contra `caja_turno.row_version`.

Si la version no coincide:

- responde conflicto seguro;
- no cambia estado;
- no crea movimiento;
- no completa idempotencia.

## Estados

Estados usados:

```text
Abierto -> EnCierre -> Cerrado
```

La implementacion:

- bloquea el turno con `UPDLOCK, HOLDLOCK`;
- valida que este `Abierto`;
- cambia temporalmente a `EnCierre`;
- calcula efectivo esperado;
- cierra con datos finales en la misma transaccion.

## Bloqueo de operaciones

Ingresos y retiros API ya resuelven turno con estado `Abierto`. Por lo tanto, un turno `EnCierre` o `Cerrado` no acepta nuevos ingresos/retiros.

Las futuras ventas efectivo deben aplicar la misma regla antes de insertar `VentaEfectivo`.

## Concurrencia

Casos cubiertos por diseno:

- dos cierres simultaneos;
- cierre con `rowVersion` desactualizada;
- cierre repetido con misma key/hash;
- cierre repetido con misma key/hash distinto;
- cierre posterior con otra key cuando el turno ya no esta `Abierto`;
- ingreso/retiro que intenta iniciar despues de `EnCierre`.

## Rollback

Si ocurre error antes de commit:

- no queda turno en `EnCierre`;
- no queda cierre parcial;
- no queda `CierreDiferencia`;
- no queda idempotencia `Completada`.
