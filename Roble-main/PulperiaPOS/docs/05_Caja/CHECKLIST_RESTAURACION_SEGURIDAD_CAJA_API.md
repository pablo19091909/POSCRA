# Checklist de restauracion de seguridad Caja API

Fecha/hora UTC: 2026-06-28 18:02:40 UTC.

## Restauracion

- `EnableCajaApiWrite=true` se uso solo como variable de entorno temporal del proceso.
- POS.Api fue detenido al finalizar.
- El puerto `7046` quedo libre.
- No se dejo API ejecutandose.
- No se modifico ningun archivo versionado para activar escritura.
- No se modifico WPF.

## Flags finales

Confirmado:

- `UseVentasApiWrite=false`
- `EnableVentasApiWrite=false`
- `EnableCajaApiWrite=false`

## Datos conservados

Se conservan como evidencia de Test:

- un turno `CAJA_PRINCIPAL_TEST` en `caja_turno`;
- un movimiento `FondoInicial` asociado en `movimiento_caja`.

No se ejecuto rollback ni eliminacion.

## Seguridad

No se documentaron ni imprimieron:

- JWT;
- contrasenas;
- hashes;
- connection strings;
- servidor;
- base;
- usuario individual;
- cuerpos completos de request.

## Pendientes

- Idempotencia persistente de caja.
- Ingresos y retiros API.
- Pre-cierre y cierre API.
- Integracion futura de ventas efectivas con `movimiento_caja`.
- Estrategia de anulacion/reversa.
