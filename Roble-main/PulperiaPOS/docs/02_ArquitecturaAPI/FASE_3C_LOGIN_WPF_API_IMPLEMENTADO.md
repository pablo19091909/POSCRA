# Fase 3C - Login WPF por POS.Api con feature flag

Fecha UTC: 2026-06-25 23:57:31 UTC

## Objetivo

Se agrego autenticacion por `POS.Api` al login WPF mediante un feature flag reversible.

El modo por defecto sigue siendo el login SQL directo existente:

- `FeatureFlags:UseApiLogin=false`.
- `LoginWindow` mantiene la consulta SQL previa.
- `DBConnection.cs` no fue modificado.

El modo API queda disponible solo cuando una configuracion local active:

- `FeatureFlags:UseApiLogin=true`.
- `Api:BaseUrl` apuntando a POS.Api por HTTPS.

## Archivos creados

- `PulperiaPOS/ApiClients/ApiClientBase.cs`
- `PulperiaPOS/ApiClients/AuthApiClient.cs`
- `PulperiaPOS/ApiClients/ApiAuthenticationState.cs`
- `PulperiaPOS/Configuration/AppConfiguration.cs`
- `PulperiaPOS/Configuration/FeatureFlags.cs`
- `PulperiaPOS/Models/Auth/ApiErrorResponse.cs`
- `PulperiaPOS/Models/Auth/AuthApiFailure.cs`
- `PulperiaPOS/Models/Auth/AuthApiResult.cs`
- `PulperiaPOS/Models/Auth/AuthenticatedUser.cs`
- `PulperiaPOS/Models/Auth/LoginRequest.cs`
- `PulperiaPOS/Models/Auth/LoginResponse.cs`

## Archivos modificados

- `PulperiaPOS/LoginWindow.xaml.cs`
- `PulperiaPOS/UserSession.cs`
- `PulperiaPOS/VentanaAdministrador.xaml.cs`
- `PulperiaPOS/VentanaAnfitrion.xaml.cs`
- `PulperiaPOS/appsettings.json`
- `PulperiaPOS/appsettings.Development.json.example`

## Configuracion agregada

`PulperiaPOS/appsettings.json` define valores no sensibles:

```json
{
  "Api": {
    "BaseUrl": "https://localhost:7046/"
  },
  "FeatureFlags": {
    "UseApiLogin": false
  }
}
```

`UseApiLogin` queda en `false` por defecto.

La URL local real puede ajustarse en `PulperiaPOS/appsettings.Development.json`, archivo cubierto por `.gitignore`.

## Funcionamiento con UseApiLogin=false

`LoginWindow` usa el flujo SQL existente:

1. Lee usuario y contrasena desde la UI.
2. Calcula SHA-256 con `Seguridad.HashContrasena`.
3. Consulta `usuario` mediante `DBConnection.GetConnection()`.
4. Llena `UserSession.IdUsuario`, `UserSession.NombreUsuario` y `UserSession.RolUsuario`.
5. Abre la ventana por rol existente.

No se elimino el flujo legado.

## Funcionamiento con UseApiLogin=true

`LoginWindow` usa `AuthApiClient`:

1. Envia JSON a `POST /api/auth/login`.
2. Recibe `accessToken`, `expiresAtUtc`, `user.id`, `user.username`, `user.role` y `permissions`.
3. Guarda el token solo en memoria en `UserSession.AccessToken`.
4. Guarda expiracion y permisos en `UserSession`.
5. Mantiene la apertura de ventanas por rol actual.

El token no se guarda en archivos, logs, base de datos ni configuracion.

## Manejo seguro de errores

El login API muestra mensajes seguros para:

- credenciales invalidas;
- limite de intentos;
- API no disponible;
- error de red;
- configuracion incompleta;
- respuesta invalida.

No se muestran stack traces, detalles internos, connection strings, JWT, usuarios, hashes ni contrasenas.

## Resultado de validacion

| Validacion | Resultado |
|---|---|
| Compilacion solucion completa | Correcta, 0 errores |
| Compilacion WPF | Correcta, 0 errores |
| Compilacion POS.Api | Correcta, 0 errores |
| `/health` | HTTP 200 |
| `/health/database` | HTTP 200 |
| `/api/system/version` | HTTP 200 |
| `Authentication:EnableLegacyHashUpgrade` local | `false` |
| `.gitignore` cubre appsettings locales | Confirmado |
| Rate limiting con credenciales ficticias | 401 inicial y 429 al exceder intentos |

La solucion conserva advertencias existentes del WPF. No se limpiaron advertencias en esta fase.

## Pruebas manuales pendientes

Por seguridad no se solicitaron, imprimieron ni almacenaron credenciales reales. Quedan para ejecucion manual del operador:

- login WPF con `UseApiLogin=false` y usuario real;
- login WPF con `UseApiLogin=true` y usuario SHA-256 legado;
- login WPF con `UseApiLogin=true` y usuario BCrypt;
- confirmacion visual de apertura de ventana por rol;
- confirmacion visual de logout y limpieza de sesion.

## Confirmaciones

- No se modifico base de datos.
- No se ejecutaron scripts SQL.
- No se modificaron hashes ni contrasenas.
- `Authentication:EnableLegacyHashUpgrade` sigue en `false`.
- No se modificaron ventas, inventario, clientes, caja, cierres, donaciones, reportes ni tipo de cambio.
- No se guardaron tokens ni secretos en archivos versionados.
- `DBConnection.cs` no fue modificado.

## Riesgos pendientes

- WPF aun mantiene acceso SQL directo para modulos funcionales.
- `VentanaUsuarios` aun gestiona hashes legados directamente.
- El token JWT aun no se usa para autorizacion de modulos WPF.
- La prueba con credenciales reales debe ejecutarse manualmente sin documentar secretos.

## Recomendacion

Ejecutar la Fase 3C.1 de validacion operativa: activar `UseApiLogin=true` solo en `PulperiaPOS/appsettings.Development.json`, probar login real con un usuario SHA-256 y un usuario BCrypt, confirmar logout, revertir `UseApiLogin=false`, y documentar resultados sin revelar credenciales ni tokens.
