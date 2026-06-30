# Maquina de estados turno Caja API

Fecha UTC: 2026-06-29 21:12:41 UTC

## Estados reales

El constraint actual permite:

- `Abierto`;
- `EnCierre`;
- `Cerrado`;
- `Anulado`.

Para el cierre API gradual se disena solo:

```text
Abierto -> EnCierre -> Cerrado
```

`Anulado` queda fuera del flujo de cierre hasta una fase especifica.

## Reglas por estado

`Abierto`:

- permite ingresos API;
- permite retiros API;
- permite futuras ventas efectivo integradas;
- permite pre-cierre;
- permite iniciar cierre con `Caja.Cerrar`.

`EnCierre`:

- bloquea ingresos;
- bloquea retiros;
- bloquea futuras ventas efectivo;
- bloquea ajustes;
- bloquea reversas salvo politica futura explicita;
- permite completar la transaccion de cierre que ya posee el bloqueo.

`Cerrado`:

- bloquea todo movimiento financiero nuevo;
- no permite volver a `Abierto`;
- no permite editar movimientos previos;
- correcciones futuras deben ser reversas formales o un nuevo turno.

## Casos de concurrencia

Dos cierres simultaneos:

- uno debe bloquear el turno y pasar a `EnCierre`;
- el otro debe recibir conflicto seguro o resolver idempotencia si es la misma key/hash.

Ingreso/retiro iniciado mientras comienza cierre:

- si el cierre obtiene el bloqueo primero, ingreso/retiro debe fallar por estado;
- si ingreso/retiro confirma primero, el cierre calcula con ese movimiento incluido.

Cierre repetido con misma key:

- si la operacion quedo `Completada`, debe devolver el mismo resultado seguro;
- si esta `EnProceso`, debe responder conflicto temporal.

Cierre con key distinta despues de cierre confirmado:

- debe responder conflicto por turno no abierto.

`rowVersion` desactualizada:

- debe responder conflicto, sin escritura.

Turno ya `EnCierre`:

- debe bloquear nuevas operaciones y permitir solo recuperacion controlada futura.

Turno ya `Cerrado`:

- no permite otro cierre ni movimientos.

Fallo antes de commit:

- rollback completo;
- no debe quedar `Cerrado` sin datos completos;
- no debe quedar idempotencia `Completada` sin cierre real.

## Invariantes

- No editar movimientos previos para corregir diferencia.
- No insertar en `cierre_caja` historico.
- No cerrar sin `usuario_cierre_id`, `cierre_utc`, `efectivo_esperado`, `efectivo_contado` y `diferencia`.
- No permitir movimientos nuevos en `EnCierre` o `Cerrado`.
