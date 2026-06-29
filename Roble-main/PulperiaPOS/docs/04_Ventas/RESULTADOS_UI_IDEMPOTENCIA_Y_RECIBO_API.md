# Resultados UI, idempotencia y recibo API

Fecha/hora de cierre: 2026-06-28 11:08 UTC.

## Resultado UI

Durante la validacion manual de `VentasPage` se confirmo:

- El indicador `API Test` aparece cuando la escritura API esta activa en WPF.
- Los metodos soportados en V1 pueden seleccionarse sin que clientes de prueba fuercen automaticamente `Saldo Cliente`.
- `Saldo Cliente` valida saldo insuficiente antes de enviar la venta.
- `Cliente General` queda disponible como opcion valida y no bloquea la venta API por identificador `0`.
- Dolares permanece bloqueado para Venta API V1.
- Donacion y pagos combinados no fueron usados.

## Resultado de errores visibles

- Stock insuficiente muestra error seguro.
- Saldo insuficiente muestra error seguro.
- API deshabilitada bloquea la venta cuando el flag de servidor esta apagado.
- No se observo fallback automatico a SQL cuando la ruta API rechazo una operacion.

## Idempotencia

Resultado agregado de cierre:

| Indicador | Resultado |
| --- | ---: |
| Idempotencias completadas | 10 |
| Facturas con mas de una idempotencia completada | 0 |
| Pagos duplicados por factura API | 0 |
| Reintentos que crearon mas de una venta | 0 |

No se imprimieron ni documentaron llaves de idempotencia. Las llaves se consideran datos tecnicos sensibles de operacion y no deben persistirse fuera del flujo normal de la aplicacion.

## Recibo

- El recibo se genera despues de que POS.Api responde exito.
- El recibo no se genera para errores de stock, saldo, autenticacion o escritura deshabilitada.
- Cliente General no rompe la generacion del recibo.
- Los recibos API aun no representan movimientos auditables de caja hasta implementar CajaTurno y MovimientoCaja.

## Resultado final

UI, idempotencia y recibo quedan validados para Venta API V1 en ambiente Test, con escritura apagada al cierre.
