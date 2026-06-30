# Revision atomicidad cierre turno

Fecha UTC: 2026-06-29 22:57:11 UTC

## Revision estatica

El flujo real de `CerrarTurnoAsync` en `CajaRepository` fue revisado y confirma:

- una sola `SqlConnection`;
- una sola `SqlTransaction`;
- aislamiento `Serializable`;
- bloqueo de idempotencia con `UPDLOCK, HOLDLOCK`;
- bloqueo de turno con `UPDLOCK, HOLDLOCK`;
- validacion de `rowVersion` dentro de la transaccion;
- cambio temporal `Abierto -> EnCierre` dentro de la transaccion;
- calculo de efectivo esperado bajo bloqueo;
- creacion opcional de `CierreDiferencia` dentro de la transaccion;
- actualizacion final a `Cerrado` dentro de la transaccion;
- idempotencia `Completada` dentro de la transaccion;
- `CommitAsync` solo al final;
- rollback ante errores.

## No toca historicos

El cierre API no inserta ni actualiza:

- `cierre_caja`;
- `ingreso_caja`;
- `retiro_caja`;
- ventas;
- pagos;
- inventario;
- cliente.

## Rollback esperado

Ante fallo antes del commit:

- no debe quedar `EnCierre`;
- no debe quedar cierre parcial;
- no debe quedar `CierreDiferencia`;
- no debe quedar idempotencia `Completada`;
- no debe tocar historicos.

## Observaciones

No se encontro bloqueo logico que impida preparar la prueba de cierre exacto. La escritura sigue bloqueada por flag y no fue ejecutada en esta fase.
