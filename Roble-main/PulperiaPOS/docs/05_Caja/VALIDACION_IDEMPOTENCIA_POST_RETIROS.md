# Validacion de idempotencia post retiros

Fecha UTC: 2026-06-29 21:02:52 UTC

## Resultado agregado

| Validacion | Resultado |
| --- | ---: |
| Idempotencias Caja API totales | 4 |
| Idempotencias Completada | 4 |
| IngresoCaja Completada | 2 |
| RetiroCaja Completada | 2 |
| EnProceso | 0 |
| Fallida | 0 |
| Completada sin movimiento | 0 |
| Multiples completadas por movimiento | 0 |
| Duplicados por usuario + operacion + key | 0 |
| Ingreso/retiro sin idempotencia | 0 |
| Idempotencia completada de intento insuficiente | 0 |

## Evidencia de comportamiento

- La repeticion con la misma key y la misma intencion no duplico la operacion.
- La misma key con una intencion distinta fue rechazada sin crear movimiento.
- El intento de retiro mayor al efectivo disponible no genero movimiento ni idempotencia completada.
- Las dos solicitudes concurrentes de retiro produjeron solo una operacion persistida.
- La otra solicitud concurrente recibio una respuesta segura y no sobregiro caja.

## Seguridad

No se documentan ni imprimen:

- Idempotency-Key;
- request hash;
- JWT;
- usuario;
- identificadores internos;
- cuerpo de solicitudes.

## Conclusion

La idempotencia posterior a retiros quedo consistente: cada movimiento de ingreso/retiro API tiene una unica idempotencia completada y no existen operaciones duplicadas ni estados intermedios pendientes.
