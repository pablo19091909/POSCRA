# Fase 3D - Cliente API autenticado para WPF

Fecha UTC: 2026-06-26 01:33:41 UTC

## Objetivo

Se creo infraestructura centralizada para futuras llamadas autenticadas desde WPF hacia `POS.Api`.

Esta fase no migra modulos de negocio. Clientes, inventario, ventas, caja, cierres, reportes, donaciones, tipo de cambio y usuarios siguen usando SQL directo.

## Componentes implementados

- `PulperiaPOS/ApiClients/ApiClientBase.cs`
- `PulperiaPOS/ApiClients/ApiSessionCoordinator.cs`
- `PulperiaPOS/ApiClients/ApiSessionExpiredEventArgs.cs`
- `PulperiaPOS/ApiClients/ApiSessionNavigationCoordinator.cs`
- `PulperiaPOS/Models/Api/ApiErrorType.cs`
- `PulperiaPOS/Models/Api/ApiRequestResult.cs`
- `PulperiaPOS/Models/Api/ApiSafeMessages.cs`
- `PulperiaPOS/Security/PermissionHelper.cs`

Tambien se amplio:

- `PulperiaPOS/UserSession.cs`
- `PulperiaPOS/App.xaml.cs`
- `PulperiaPOS/LoginWindow.xaml.cs`
- `PulperiaPOS/appsettings.json`
- `PulperiaPOS/appsettings.Development.json.example`

## Arquitectura

`ApiClientBase` concentra:

- URL base desde configuracion;
- `HttpClient`;
- timeout;
- encabezado `Accept: application/json`;
- serializacion JSON;
- inyeccion opcional de Bearer token;
- manejo uniforme de respuestas HTTP;
- cancelacion mediante `CancellationToken`;
- resultado tipado `ApiRequestResult<T>`.

Los futuros clientes como `ClientesApiClient`, `InventarioApiClient`, `VentasApiClient` y `CajaApiClient` deben heredar de `ApiClientBase` y llamar `SendAsync<T>()`.

Ejemplo conceptual:

```csharp
return await SendAsync<ClienteDto>(
    HttpMethod.Get,
    "api/clientes/1",
    requiresAuthentication: true,
    cancellationToken: cancellationToken);
```

## Configuracion

Configuracion versionada no sensible:

```json
{
  "Api": {
    "BaseUrl": "https://localhost:7046/",
    "RequestTimeoutSeconds": 30
  },
  "FeatureFlags": {
    "UseApiLogin": false
  }
}
```

`UseApiLogin` permanece `false` por defecto.

No se agregaron connection strings, tokens, passwords ni secretos.

## Inyeccion de token

El token se adjunta centralmente solo cuando:

- la llamada declara `requiresAuthentication=true`;
- `UserSession.IsApiAuthenticated=true`;
- `UserSession.AccessToken` tiene valor;
- `UserSession.TokenExpiresAtUtc` existe;
- el token no esta vencido localmente.

Si el token falta o esta vencido:

- no se envia la peticion autenticada;
- se limpia `UserSession`;
- se notifica sesion expirada;
- no hay fallback automatico hacia SQL.

El header `Authorization` no se registra ni se documenta.

## Manejo HTTP

| Estado | Comportamiento |
|---|---|
| 200-299 | Deserializa y entrega `ApiRequestResult<T>.Succeeded`. |
| 400 | Devuelve mensaje funcional seguro. |
| 401 | Limpia sesion y notifica sesion expirada. |
| 403 | Conserva sesion y devuelve mensaje de permiso. |
| 429 | Conserva sesion y devuelve mensaje de espera. |
| Timeout/red | Devuelve mensaje seguro sin host, puerto ni stack trace. |
| 500/503 | Devuelve error de servicio seguro y conserva solo `traceId` si viene. |

## Permisos WPF

`UserSession.HasPermission()` y `PermissionHelper.HasPermission()` quedan disponibles para UX futura.

Estos permisos solo deben usarse para ocultar o deshabilitar elementos visuales. `POS.Api` es la autoridad final de autorizacion cuando cada modulo sea migrado.

## Pruebas realizadas

| Prueba | Resultado |
|---|---|
| Compilacion solucion completa | Correcta, 0 errores |
| `/health` | HTTP 200 |
| `/health/database` | HTTP 200 |
| `/api/system/version` | HTTP 200 |
| `/api/auth/me` sin token | HTTP 401 controlado |
| Rate limiting con credenciales ficticias | HTTP 401 y luego HTTP 429 |
| Integridad usuarios agregada | 12 usuarios, 1 BCrypt, 12 hashes legados presentes |
| `Authentication:EnableLegacyHashUpgrade` | `false` |
| `UseApiLogin` local | `false` |

No se ejecuto login exitoso con usuarios reales en esta fase para evitar uso innecesario de credenciales.

## Confirmaciones

- No se modifico `DBConnection.cs`.
- No se modifico `POS.Api` funcionalmente.
- No se modifico base de datos.
- No se ejecutaron scripts SQL ni migraciones.
- No se migraron modulos de negocio.
- No se agrego fallback automatico API -> SQL.
- No se guardaron tokens ni secretos.

## Riesgos pendientes

- No existe refresh token.
- Los modulos funcionales siguen usando SQL directo.
- La expiracion de token solo afectara pantallas futuras que usen la infraestructura API.
- Aun no existen clientes API de negocio.

## Recomendacion

Ejecutar Fase 3E: crear el primer cliente API de lectura para un modulo de bajo riesgo, con endpoint de solo consulta en `POS.Api`, manteniendo SQL directo como comportamiento productivo hasta validar el modulo.
