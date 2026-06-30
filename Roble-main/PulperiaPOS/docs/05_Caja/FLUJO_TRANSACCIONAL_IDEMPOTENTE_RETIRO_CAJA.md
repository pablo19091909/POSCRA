# Flujo transaccional idempotente RetiroCaja

Fecha UTC: 2026-06-29 16:07:09 UTC

## Flujo futuro

```text
Validar JWT y permiso Caja.Retirar
-> validar EnableCajaApiWrite y Environment=Test
-> validar request basico
-> obtener Idempotency-Key
-> calcular request hash canonico RetiroCaja
-> abrir una conexion
-> iniciar transaccion Serializable
-> bloquear idempotencia por usuario + operacion + key
-> resolver repeticion, conflicto o EnProceso
-> bloquear turno Abierto de caja
-> calcular efectivo disponible desde movimiento_caja
-> validar monto <= disponible
-> crear caja_idempotencia EnProceso
-> crear movimiento_caja RetiroCaja Confirmado
-> completar caja_idempotencia con idMovimiento/idTurno
-> responder resultado seguro
-> commit
```

## Operacion

Operacion idempotente:

```text
RetiroCaja
```

Movimiento:

```text
tipo_movimiento = RetiroCaja
origen = POS.Api
estado = Confirmado
moneda = CRC
```

## Hash canonico

Campos:

- operacion fija `RetiroCaja`;
- usuario autenticado;
- caja normalizada;
- monto normalizado con cultura invariante y dos decimales;
- motivo normalizado;
- referencia normalizada.

Reglas:

- no incluir timestamps;
- no incluir token;
- no incluir efectivo disponible;
- no incluir ids generados;
- no guardar request completo;
- no exponer hash ni key.

## Aislamiento historico

El flujo no debe insertar ni actualizar:

- `retiro_caja`;
- `ingreso_caja`;
- `cierre_caja`;
- ventas;
- pagos;
- inventario;
- cliente.

## Estado actual

El flujo transaccional real aun no esta implementado. La Fase 4F.14 deja preparado contrato/hash y endpoint bloqueado.
