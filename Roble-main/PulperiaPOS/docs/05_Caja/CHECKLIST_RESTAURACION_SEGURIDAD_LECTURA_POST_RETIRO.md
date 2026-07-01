# Checklist restauracion seguridad lectura post retiro

Fecha UTC: 2026-06-30

## Flags al cierre

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

- Sin escrituras durante Fase 4F.33.
- Sin rollback.
- Turno Test abierto conservado.
- Movimiento de ingreso Test conservado.
- Movimiento de retiro Test conservado.

## Seguridad

- No se imprimieron tokens.
- No se documentaron connection strings.
- No se documentaron usuarios ni datos personales.
- No se mostro `rowVersion`.
- No se expusieron hosts ni puertos en la UI validada.

## Pendientes para siguiente fase

- Migrar/preparar `CierreCajaPage` para lectura de pre-cierre desde Caja API.
- Mantener cierre API write apagado.
- Validar permisos negativos visuales cuando exista cuenta adecuada sin `Caja.Ver`.
