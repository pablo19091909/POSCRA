# Resultados de atomicidad y concurrencia - Apertura Caja API

Fecha/hora UTC: 2026-06-28 18:02:40 UTC.

## Atomicidad

La apertura real creo exactamente:

- un registro en `dbo.caja_turno`;
- un registro relacionado en `dbo.movimiento_caja`;
- movimiento de tipo `FondoInicial`;
- relacion valida por `idTurno`.

No se encontraron:

- movimientos huerfanos;
- turnos sin movimiento inicial;
- fondos iniciales duplicados;
- referencias a factura, pago, ingreso, retiro o reversa;
- montos cero o negativos.

## Transaccion

La operacion se ejecuto mediante POS.Api, usando la transaccion interna preparada en la fase anterior.

La evidencia posterior confirma que turno y movimiento quedaron juntos. No se detecto estado parcial.

## Concurrencia

Se ejecuto una segunda solicitud controlada sobre:

```text
CAJA_PRINCIPAL_TEST
```

Resultado:

- HTTP 409;
- se conservo un unico turno abierto;
- se conservo un unico `FondoInicial`;
- no se alteraron tablas historicas.

## Integridad historica

Sin cambios en:

- `ingreso_caja`;
- `retiro_caja`;
- `cierre_caja`;
- ventas;
- pagos;
- inventario agregado;
- saldo agregado.

## Resultado

La prueba confirma la creacion atomica del turno y su movimiento inicial, y confirma que la proteccion de conflicto evita una segunda apertura para la misma caja logica.
