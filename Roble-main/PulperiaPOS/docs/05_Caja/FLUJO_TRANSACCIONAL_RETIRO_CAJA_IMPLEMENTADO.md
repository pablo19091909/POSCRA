# Flujo transaccional RetiroCaja implementado

Fecha UTC: 2026-06-29 16:17:55 UTC

## Secuencia

```text
POST /api/caja/retiros
-> Authorize Caja.Retirar
-> CanWriteCajaAsync
-> ValidateRetiro
-> TryParse Idempotency-Key
-> ComputeRetiroRequestHash
-> CajaRepository.RegistrarRetiroAsync
-> BeginTransaction Serializable
-> UsuarioActivoExiste
-> GetCajaIdempotenciaForUpdate
-> ResolveExistingCajaIdempotencia
-> GetTurnoAbiertoIdForUpdate
-> CalcularEfectivoDisponibleEnTurno
-> CrearCajaIdempotenciaEnProceso
-> CrearMovimientoRetiroCaja
-> CompletarCajaIdempotencia
-> GetMovimientoById
-> Commit
```

## Idempotencia

Misma key y mismo hash:

- devuelve el movimiento original;
- no crea retiro adicional;
- no descuenta efectivo otra vez.

Misma key y hash distinto:

- devuelve conflicto;
- no crea movimiento;
- no cambia la idempotencia original.

Estado `EnProceso`:

- devuelve conflicto;
- no crea movimiento.

## Rollback

Cualquier excepcion antes del commit revierte:

- idempotencia `EnProceso`;
- movimiento `RetiroCaja`;
- cambios parciales.

La idempotencia y el movimiento estan en la misma transaccion.

## Aislamiento

La implementacion usa:

- `Serializable`;
- `UPDLOCK`;
- `HOLDLOCK`;
- indice unico de idempotencia;
- manejo seguro de `2601` y `2627`.

## Historicos

No se escribe en:

- `retiro_caja`;
- `ingreso_caja`;
- `cierre_caja`;
- ventas;
- pagos;
- inventario;
- clientes.
