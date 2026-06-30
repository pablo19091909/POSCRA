# Concurrencia y rowVersion cierre Caja API

Fecha UTC: 2026-06-29 21:12:41 UTC

## rowVersion

`caja_turno.row_version` existe y ya se expone como Base64 en la respuesta de turno. El cierre futuro debe exigir `rowVersion` en el request.

Regla elegida:

- `rowVersion` forma parte del hash canonico de `CerrarTurno`.

Justificacion:

- representa la version exacta del turno que el operador vio al confirmar;
- si cambia el turno por ingreso, retiro o cierre concurrente, la intencion ya no es equivalente;
- repetir la misma key con una version distinta debe ser conflicto.

## Hash canonico

Debe incluir:

- operacion fija `CerrarTurno`;
- usuario autenticado;
- identificador logico de turno;
- caja normalizada;
- efectivo contado normalizado con dos decimales y cultura invariante;
- observacion normalizada;
- `rowVersion` Base64 normalizado.

No debe incluir:

- fecha/hora;
- efectivo esperado;
- diferencia;
- token;
- datos derivados;
- request completo;
- cultura local.

Usar UTF-8 + SHA-256.

## Bloqueo de operaciones

Ingreso, retiro y futura venta efectivo deben validar que el turno este `Abierto` dentro de su transaccion. Si el turno esta `EnCierre` o `Cerrado`, deben fallar sin escritura.

El cierre debe:

- tomar bloqueo del turno;
- validar `rowVersion`;
- pasar a `EnCierre`;
- calcular efectivo;
- cerrar en la misma transaccion.

## Casos

Dos cierres simultaneos:

- uno confirma;
- otro recibe conflicto por estado, `rowVersion` o idempotencia.

Ingreso/retiro simultaneo:

- si confirma antes del cierre, entra en el calculo;
- si el cierre pasa a `EnCierre` primero, queda bloqueado.

Misma key y mismo hash:

- respuesta repetida segura.

Misma key y hash distinto:

- conflicto.

Turno ya cerrado:

- conflicto, sin crear idempotencia completada nueva.

Fallo antes de commit:

- rollback; no hay cierre parcial.

## Pendiente tecnico

El repository aun no implementa `CerrarTurno` real. La siguiente fase debe agregar metodos transaccionales especificos y pruebas sin activar el flag por defecto.
