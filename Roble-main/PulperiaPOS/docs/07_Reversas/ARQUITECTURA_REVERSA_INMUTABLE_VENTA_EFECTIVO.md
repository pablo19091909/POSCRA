# Arquitectura - Reversa inmutable de venta en efectivo

## Objetivo

Permitir en una fase futura reversar totalmente una venta pagada 100% en efectivo, integrada con Caja API, sin borrar ni editar la evidencia original.

## Principios

- La venta original no se elimina.
- El detalle original no se elimina.
- El pago original no se elimina.
- El movimiento `VentaEfectivo` original no se elimina ni edita.
- La reversa se representa con registros compensatorios auditables.
- Toda ejecucion real debe ocurrir dentro de una unica `SqlConnection` y una unica `SqlTransaction`.
- La idempotencia debe evitar duplicados ante doble click, reintentos o fallos intermedios.

## Flujo futuro propuesto

1. Validar JWT y permiso `Ventas.Reversar`.
2. Validar flags de WPF y API.
3. Validar `Environment=Test`.
4. Validar venta confirmada.
5. Validar pago unico o total en efectivo.
6. Validar que la venta pertenece al turno actualmente abierto.
7. Validar que la venta no fue reversada previamente.
8. Validar inventario recuperable.
9. Validar efectivo suficiente para compensar caja.
10. Abrir transaccion.
11. Registrar intencion idempotente.
12. Registrar reversa de venta.
13. Restaurar inventario.
14. Crear movimiento compensatorio en caja.
15. Registrar auditoria.
16. Confirmar idempotencia.
17. Commit.

## Estado actual

La superficie API existe pero esta bloqueada. No existe ejecucion real de reversa ni persistencia nueva de reversas de venta.

## Condiciones de rechazo V1

- Venta parcial.
- Venta con metodos combinados.
- Venta sin pago efectivo.
- Venta en turno cerrado.
- Venta en turno en cierre.
- Venta ya reversada.
- Reversa de reversa.
- Falta de permiso.
- Falta de idempotency key.
- Motivo vacio.
- Flags apagadas.
