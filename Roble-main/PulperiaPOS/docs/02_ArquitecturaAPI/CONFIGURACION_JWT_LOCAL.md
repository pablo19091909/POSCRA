# Configuracion JWT local

## Secciones de configuracion

`POS.Api/appsettings.json` contiene solo placeholders no funcionales.

```json
{
  "Jwt": {
    "Issuer": "POS.Api",
    "Audience": "PulperiaPOS.WPF",
    "SigningKey": "CONFIGURAR_MEDIANTE_SECRETO_LOCAL_O_VARIABLE_ENTORNO",
    "AccessTokenMinutes": 45
  },
  "Authentication": {
    "Enabled": false,
    "EnableLegacyHashUpgrade": false,
    "BcryptWorkFactor": 12
  }
}
```

## Prioridad de SigningKey

1. Variable de entorno `POS_API_JWT_SIGNING_KEY`.
2. `POS.Api/appsettings.Development.json` local ignorado.
3. Placeholder no funcional de `appsettings.json`.

La clave nunca debe imprimirse, registrarse ni documentarse.

## Arranque seguro

Si `Authentication:Enabled=true` y no existe una signing key real, `POS.Api` falla durante startup.

Esto evita ejecutar autenticacion con placeholder o clave insegura.

## Desarrollo local

Recomendado:

1. Crear `POS.Api/appsettings.Development.json` local o usar variable de entorno.
2. Configurar una clave de al menos 32 caracteres aleatorios.
3. Mantener `EnableLegacyHashUpgrade=false` hasta ejecutar migracion SQL.
4. Probar login solo con usuario de prueba.

## Produccion

Usar secreto administrado o variable segura del entorno.

No usar:

- claves hardcodeadas;
- valores reales en `appsettings.json`;
- valores reales en archivos `.example`;
- valores reales en documentacion.

## Rate limiting

Configuracion:

```json
{
  "RateLimiting": {
    "Login": {
      "PermitLimit": 5,
      "WindowSeconds": 60
    }
  }
}
```

Ajustar para desarrollo o produccion segun pruebas, sin desactivar proteccion en ambientes reales.
