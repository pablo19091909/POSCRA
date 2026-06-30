# Revision de configuracion local POS.Api

Fecha UTC: 2026-06-29 15:07:18 UTC

## Archivo revisado

`POS.Api/POS.Api.csproj`

## Resultado

El proyecto contiene reglas para evitar que estos archivos locales de entorno se copien a salida o publicacion:

- `appsettings.Development.json`;
- `appsettings.Test.json`;
- `appsettings.Production.json`.

## Confirmaciones

- La lectura de configuracion local desde el directorio del proyecto sigue funcionando al iniciar POS.Api.
- `appsettings.json` versionado no fue alterado.
- `appsettings.Test.json`, si existe localmente, no se publica ni se copia a artefactos.
- `EnableCajaApiWrite` no queda activado por defecto.
- `EnableVentasApiWrite` permanece apagado.
- Swagger, health checks y version siguen funcionando.
- WPF no fue modificado.

## Resultado de artefactos

Despues de compilar y ejecutar POS.Api, se confirmo:

- copias generadas de `appsettings.Development.json` en `POS.Api/bin` y `POS.Api/obj`: `0`.

## Nota operativa

La activacion temporal de `EnableCajaApiWrite=true` para la Fase 4F.12 se realizo por variable del proceso de POS.Api, no por cambio en archivos versionados.
