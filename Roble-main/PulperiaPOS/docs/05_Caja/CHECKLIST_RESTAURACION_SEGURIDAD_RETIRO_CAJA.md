# Checklist de restauracion de seguridad RetiroCaja

Fecha UTC: 2026-06-29 16:43:03 UTC

## Restauracion

- [x] `EnableCajaApiWrite` quedo apagado en archivos versionados/locales.
- [x] `EnableVentasApiWrite` permanece `false`.
- [x] `UseVentasApiWrite` permanece `false`.
- [x] POS.Api fue detenida.
- [x] Puerto `7046` quedo libre.
- [x] No se ejecuto rollback.
- [x] No se eliminaron registros de evidencia Test.
- [x] No se modifico WPF.
- [x] No se modificaron tablas historicas.

## Evidencia Test que permanece

- Retiro principal de `500.00`.
- Retiro de concurrencia de `800.00`.
- Dos idempotencias `RetiroCaja Completada`.

No se documentan keys, tokens, hashes, usuarios, connection strings ni identificadores internos.

## Estado final agregado

- `movimiento_caja=5`.
- `caja_idempotencia=4`.
- `retiro_caja=6`.
- `ingreso_caja=9`.
- `cierre_caja=15`.
- efectivo esperado `201.00`.
