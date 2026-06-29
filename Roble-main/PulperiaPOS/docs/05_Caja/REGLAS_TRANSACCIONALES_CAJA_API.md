# Reglas transaccionales Caja API

## Apertura

```text
Validar permiso
-> validar cajaCodigo
-> validar fondo inicial >= 0
-> validar que no hay turno abierto/en cierre
-> crear CajaTurno Abierto
-> crear MovimientoCaja FondoInicial
-> commit
```

## Ingreso

```text
Validar turno abierto
-> validar monto > 0
-> crear MovimientoCaja IngresoCaja
-> commit
```

## Retiro

```text
Validar turno abierto
-> calcular efectivo disponible dentro de transaccion
-> validar monto <= disponible
-> crear MovimientoCaja RetiroCaja
-> commit
```

## Pre-cierre

```text
Validar turno abierto
-> calcular efectivo esperado desde movimiento_caja
-> devolver resumen por tipo
-> no modificar datos
```

## Cierre

```text
Validar turno abierto
-> validar rowVersion
-> calcular efectivo esperado
-> comparar efectivo contado
-> exigir observacion cuando diferencia != 0
-> cerrar turno
-> crear MovimientoCaja CierreDiferencia si aplica
-> commit
```

## Principios

- El efectivo esperado sale de `movimiento_caja`, no de fecha calendario.
- Venta efectiva futura crea un unico movimiento neto por total de venta.
- Tarjeta, SINPE y SaldoCliente no crean efectivo fisico.
- Cliente General no tiene tratamiento especial en caja.
- Las correcciones financieras se hacen con reversas, no edicion destructiva.
