# Validacion marca entorno Test para escritura API

Fecha/hora UTC: 2026-06-26

## Objetivo

Evitar que `POST /api/ventas` pueda escribir solamente por activar un feature flag.

## Marca de base

Se creo `dbo.app_environment` mediante:

`database/test/000_MarcarEntornoTest.sql`

Marca requerida:

- `environment_name = Test`.
- `writes_allowed_for_testing = 1`.

## Proteccion en POS.Api

Componentes agregados:

- `EnvironmentSafetyOptions`.
- `IDatabaseEnvironmentSafetyService`.
- `DatabaseEnvironmentSafetyService`.

Regla:

```text
EnableVentasApiWrite = true
AND
dbo.app_environment confirma Environment=Test
```

Si no se cumple, la API responde 503 seguro.

## Configuracion

Configuracion versionada segura:

- `EnableVentasApiWrite=false`.
- `RequiredDatabaseEnvironment=Test`.
- `BlockWritesUnlessDatabaseEnvironmentMatches=true`.

Configuracion local Test usada temporalmente:

- `EnableVentasApiWrite=true` durante pruebas.
- Restaurado a `false` al cierre.

## Validacion final

- Health checks HTTP 200.
- `POST /api/ventas` final con flag restaurado: HTTP 503.
- API detenida.
- Puerto `7046` libre.
- No se imprimieron secretos, JWT ni cadenas de conexion.
