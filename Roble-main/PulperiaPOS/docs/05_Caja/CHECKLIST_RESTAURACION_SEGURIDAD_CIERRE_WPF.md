# Checklist restauracion seguridad - Cierre WPF

Fecha UTC: 2026-06-30

## Flags finales

- `UseCajaApiRead=true`.
- `UseCajaApiOpenWrite=false`.
- `UseCajaApiIngresoWrite=false`.
- `UseCajaApiRetiroWrite=false`.
- `UseCajaApiCierreWrite=false`.
- `UseVentasApiWrite=false`.
- `EnableCajaApiWrite=false`.
- `EnableVentasApiWrite=false`.
- `EnableLegacyHashUpgrade=false`.

## Procesos

- POS.Api temporal detenida.
- Puerto local configurado libre.

## Base de datos

- Turno Test abierto conservado.
- Ingreso sintetico conservado.
- Retiro sintetico conservado.
- No hubo cierre.
- No hubo rollback.
- No hubo limpieza.

## Seguridad

- No se documentaron tokens.
- No se documentaron idempotency keys.
- No se expuso `rowVersion`.
- No se imprimieron connection strings.
- No se mostraron datos personales.

## Proxima fase

Mantener cierre real bloqueado hasta una fase autorizada explicitamente.
