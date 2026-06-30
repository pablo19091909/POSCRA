# Concurrencia RetiroCaja API

Fecha UTC: 2026-06-29 16:07:09 UTC

## Objetivo

Evitar efectivo negativo, duplicados por reintento y retiros posteriores al cierre.

## Escenario A - Dos retiros simultaneos

Ejemplo:

```text
Disponible: 1501.00
Retiro A: 1000.00
Retiro B: 1000.00
```

Resultado esperado:

- solo uno completa;
- el segundo recibe error seguro de efectivo insuficiente o conflicto;
- no queda efectivo negativo;
- no se crea segundo movimiento invalido;
- no se crea segunda idempotencia `Completada`.

Control:

- transaccion `Serializable`;
- bloqueo del turno abierto;
- lectura de movimientos bajo la misma transaccion;
- calculo del disponible antes de insertar el movimiento;
- insert de idempotencia y movimiento en la misma transaccion.

## Escenario B - Ingreso y retiro simultaneos

Regla:

- las operaciones deben serializarse por turno;
- el retiro ve el ingreso si el ingreso confirma antes;
- si el retiro confirma antes, el ingreso posterior aumenta disponible despues;
- no se permite usar lecturas obsoletas fuera de transaccion.

## Escenario C - Retiro y cierre simultaneos

Regla:

- el cierre debe bloquear nuevos retiros al pasar a `EnCierre`;
- un retiro ya dentro de transaccion debe completar o revertir consistentemente;
- no puede existir retiro posterior a cierre confirmado.

## Escenario D - Misma key, mismo request

Resultado:

- primera solicitud crea un movimiento;
- repeticion devuelve resultado equivalente;
- no descuenta efectivo por segunda vez.

## Escenario E - Misma key, intencion distinta

Resultado:

- devuelve `409 Conflict`;
- no modifica la operacion original;
- no crea movimiento nuevo.

## Rollback

Si ocurre error antes de commit:

- no queda movimiento;
- no queda idempotencia completada;
- no cambia efectivo;
- no toca historicos.
