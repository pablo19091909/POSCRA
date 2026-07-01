# Arquitectura transaccional VentaEfectivo Caja

## Objetivo

Una venta API pagada en efectivo debe crear exactamente un movimiento `VentaEfectivo` y ambos deben confirmarse o revertirse juntos.

## Secuencia futura

```text
Validar JWT
-> validar permiso Ventas.Crear
-> validar flags generales y especificos
-> validar Environment=Test
-> resolver idempotencia de venta
-> bloquear turno de caja abierto
-> validar usuario
-> validar cliente
-> bloquear productos
-> calcular total decimal
-> calcular pago
-> insertar venta
-> insertar detalle
-> descontar inventario
-> insertar venta_pago
-> insertar movimiento_caja VentaEfectivo
-> insertar auditoria
-> completar venta_idempotencia
-> commit
```

## Transaccion unica

La implementacion preparada usa la misma `SqlConnection` y `SqlTransaction` del repositorio de venta. No llama a Caja API como servicio separado ni abre una segunda transaccion.

## Reglas

- Si falla caja antes del commit, no queda venta.
- Si falla venta, no queda movimiento de caja.
- Si falla inventario, no queda caja.
- Si falla pago, no queda caja.
- Si falla auditoria o idempotencia, se revierte todo.
- `ingreso_caja`, `retiro_caja` y `cierre_caja` no participan.

## Efectivo esperado

`VentaEfectivo` se incluye como aumento de efectivo esperado en Caja API. El monto preparado para el movimiento es el monto de la venta/pago registrado, no el monto recibido con vuelto.

## Caja logica

La fase mantiene caja logica fija `CAJA_PRINCIPAL_TEST` para Test porque el contrato de venta no incluye caja. Esta decision es segura para implementacion bloqueada, pero requiere definicion antes de activar en escenarios con multiples cajas.
