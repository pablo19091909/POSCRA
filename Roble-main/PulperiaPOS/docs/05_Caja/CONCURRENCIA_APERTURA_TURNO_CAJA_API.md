# Concurrencia de apertura de turno Caja API

## Objetivo

Garantizar que exista como maximo un turno `Abierto` o `EnCierre` por `caja_codigo`.

## Barreras usadas

- Transaccion SQL con aislamiento `Serializable`.
- Consulta dentro de la transaccion con `UPDLOCK` y `HOLDLOCK`.
- Indice unico filtrado `UX_caja_turno_un_abierto_por_caja`.
- Manejo controlado de errores SQL `2601` y `2627` como conflicto seguro.

## Comportamiento esperado

| Escenario | Resultado |
| --- | --- |
| Misma caja con turno abierto | 409 |
| Misma caja con turno en cierre | 409 |
| Otra caja logica | Permitida si pasa validaciones y flags |
| Dos requests concurrentes misma caja | Uno puede ganar; el otro recibe 409 |
| Error antes de commit | rollback total |
| Error despues de turno y antes de movimiento | rollback total |

## Invariantes

- No debe quedar `caja_turno` sin movimiento `FondoInicial`.
- No debe quedar `movimiento_caja` sin turno.
- No deben existir dos fondos iniciales para la misma apertura.
- La fecha oficial se genera en SQL Server como UTC.

## Pendiente

Antes de activar escritura real se recomienda agregar idempotencia especifica de caja para reintentos de red.
