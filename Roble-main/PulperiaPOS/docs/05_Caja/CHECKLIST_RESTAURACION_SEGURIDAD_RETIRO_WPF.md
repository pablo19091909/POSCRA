# Checklist de restauracion y seguridad - Retiros WPF

Fecha UTC: 2026-06-30

Actualizacion Fase 4F.32: despues de la prueba controlada de retiro API, la configuracion local fue restaurada y POS.Api temporal fue detenida.

## Flags esperados al cierre

- `UseCajaApiRead=true`.
- `UseCajaApiOpenWrite=false`.
- `UseCajaApiIngresoWrite=false`.
- `UseCajaApiRetiroWrite=false`.
- `UseCajaApiCierreWrite=false`.
- `UseVentasApiWrite=false`.
- `EnableCajaApiWrite=false`.
- `EnableVentasApiWrite=false`.
- `EnableLegacyHashUpgrade=false`.

## Base de datos

- No ejecutar scripts.
- No ejecutar migraciones.
- No ejecutar inserts manuales.
- No ejecutar updates manuales.
- No ejecutar deletes manuales.
- Validar solo con consultas agregadas.

## Seguridad

- No documentar secretos.
- No imprimir cadenas de conexion.
- No imprimir tokens.
- No guardar tokens.
- No habilitar CORS abierto.
- No cambiar credenciales locales.

## Procesos

- Detener POS.Api temporal al finalizar la fase.
- Confirmar que el puerto local configurado queda libre.
- Cerrar la instancia WPF temporal si fue abierta para la prueba.

## Estado recomendado antes de Fase 4F.32

- Caja API lectura activa.
- Retiro API escritura apagada.
- API general de caja escritura apagada hasta la prueba controlada.
- Ventas API escritura apagada.
- Una sola sesion operativa de WPF para la prueba manual.
- Validacion agregada antes y despues.

## Estado confirmado despues de Fase 4F.32

- `UseCajaApiRead=true`.
- `UseCajaApiOpenWrite=false`.
- `UseCajaApiIngresoWrite=false`.
- `UseCajaApiRetiroWrite=false`.
- `UseCajaApiCierreWrite=false`.
- `UseVentasApiWrite=false`.
- POS.Api temporal detenida.
- Puerto local configurado libre.
- Retiro sintetico Test conservado como evidencia.
- Sin rollback.
