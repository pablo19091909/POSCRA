# Control de efectivo disponible para RetiroCaja

Fecha UTC: 2026-06-29 16:17:55 UTC

## Fuente de verdad

El efectivo disponible de Caja API se calcula exclusivamente desde `movimiento_caja`.

No usa:

- `ventas`;
- `ingreso_caja`;
- `retiro_caja`;
- `cierre_caja`;
- fecha local;
- valores enviados por cliente.

## Formula

```text
Disponible =
  FondoInicial
+ VentaEfectivo
+ IngresoCaja
+ AjustePositivo
- RetiroCaja
- AjusteNegativo
- DevolucionEfectivo
```

Las reversas quedan preparadas para una fase futura.

## Implementacion

`CajaRepository` contiene una funcion interna transaccional que calcula el disponible dentro de la misma conexion y transaccion del retiro.

Propiedades:

- aislamiento `Serializable`;
- `UPDLOCK, HOLDLOCK` sobre `movimiento_caja`;
- filtro por `idTurno`;
- filtro por estado `Confirmado`;
- tipo `decimal`;
- sin SQL dinamico.

## Regla de negocio

Un retiro se permite solo si:

- el turno existe;
- el turno esta `Abierto`;
- el usuario esta activo;
- el monto es positivo;
- el monto es menor o igual al disponible calculado bajo lock;
- la operacion tiene idempotency key valida.

Si el monto excede el disponible, se devuelve conflicto seguro y no se crea movimiento ni idempotencia completada.

## Estado Test actual

```text
FondoInicial: 1000.00
IngresosCaja: 501.00
RetirosCaja: 0.00
Disponible: 1501.00
```
