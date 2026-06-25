# Configuracion local de POS.Api

## Archivos

- `POS.Api/appsettings.json`: configuracion no sensible. No debe contener secretos.
- `POS.Api/appsettings.Development.json.example`: plantilla con placeholders. Puede versionarse.
- `POS.Api/appsettings.Development.json`: archivo local real. No debe versionarse ni compartirse.

## Prioridad de conexion

La API resuelve `ConnectionStrings:PosDatabase` en este orden:

1. Variable de entorno `POS_API_DATABASE_CONNECTION_STRING`.
2. `POS.Api/appsettings.Development.json` local, si existe.
3. `POS.Api/appsettings.json` solo como fallback no sensible.

## Reglas de seguridad

- No imprimir cadenas de conexion.
- No registrar password, usuario, servidor ni base de datos.
- No copiar configuracion local de WPF hacia `POS.Api`.
- No commitear `appsettings.Development.json` ni `appsettings.Production.json`.
- Mantener `appsettings.Development.json.example` solo con placeholders.

## CORS

La politica configurada es `WpfLocalClient`. Los origenes permitidos se leen desde `Cors:AllowedOrigins`.

No se usa `AllowAnyOrigin`.

## Validacion local

Para validar sin exponer secretos, ejecutar la API con la variable de entorno configurada en la sesion local y probar:

- `GET /health`
- `GET /api/system/version`
- `GET /health/database`

Si `/health/database` falla, la API debe devolver 503 con mensaje seguro.
