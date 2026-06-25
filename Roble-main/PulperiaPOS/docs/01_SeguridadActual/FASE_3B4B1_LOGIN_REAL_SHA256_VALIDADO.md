# Fase 3B.4B-1 - Login real SHA-256 validado

## Fecha y hora UTC

Validacion cerrada: `2026-06-25T03:19:26Z`.

## Precondiciones

| Validacion | Resultado |
| --- | --- |
| `Authentication:Enabled` | `true` |
| `Authentication:EnableLegacyHashUpgrade` | `false` |
| JWT signing key local/segura | Presente, sin revelar valor |
| `Jwt:Issuer` | Configurado |
| `Jwt:Audience` | Configurado |
| `/health` | HTTP 200 |
| `/health/database` | HTTP 200 |
| `/api/system/version` | HTTP 200 |

## Integridad previa

| Metrica | Antes |
| --- | --- |
| Total usuarios | 12 |
| `password_hash_v2` NULL | 12 |
| `password_hash_v2` con valor | 0 |
| `password_hash_version` NULL | 12 |
| `password_migrada_utc` NULL | 12 |
| `ultimo_login_utc` NULL | 12 |
| Usuarios activos | 12 |
| `intentos_fallidos = 0` | 12 |
| Roles distintos | 2 |

## Prueba manual del operador

El operador confirmo que la prueba manual real desde Swagger fue correcta.

Resultados confirmados por operador:

| Paso | Resultado |
| --- | --- |
| `POST /api/auth/login` con cuenta real autorizada | HTTP 200 |
| Respuesta con `accessToken`, `expiresAtUtc`, `user.id`, `user.role`, permisos | Correcto |
| Authorize temporal en Swagger con Bearer token | Correcto |
| `GET /api/auth/me` | HTTP 200 |
| Prueba de permiso presente | HTTP 200 |
| Prueba de permiso ausente | HTTP 403 |
| Token retirado de Swagger al terminar | Confirmado |

No se registraron ni documentaron username, contrasena, JWT, hashes, signing key ni connection string.

## Validaciones posteriores de JWT y permisos

| Prueba | Resultado |
| --- | --- |
| Token ausente en `/api/auth/me` | HTTP 401 |
| Token manipulado en `/api/auth/me` | HTTP 401 |
| Rate limit en `/api/auth/login` | HTTP 429 |
| `/health` despues de rate limit | HTTP 200 |

## Integridad posterior

| Metrica | Despues |
| --- | --- |
| Total usuarios | 12 |
| `password_hash_v2` NULL | 12 |
| `password_hash_v2` con valor | 0 |
| `password_hash_version` NULL | 12 |
| `password_migrada_utc` NULL | 12 |
| `ultimo_login_utc` NULL | 12 |
| Usuarios activos | 12 |
| `intentos_fallidos = 0` | 12 |
| Password heredado presente | 12 |
| Roles distintos | 2 |

## Confirmaciones

- `EnableLegacyHashUpgrade` permanecio en `false`.
- No se escribio ningun hash BCrypt.
- No se modifico `usuario.contrasena`.
- No se modificaron passwords.
- No se modificaron roles.
- No se modifico WPF.
- No se modificaron datos de negocio.
- No se ejecutaron scripts SQL.
- No se ejecutaron migraciones ni rollback.
- No se revelaron secretos.

## Compilacion

La solucion completa compilo correctamente:

- `PulperiaPOS`: correcto.
- `POS.Api`: correcto.
- 0 errores.
- 0 advertencias.

## Riesgos pendientes

- Los 12 usuarios siguen usando SHA-256 legado.
- `password_hash_v2` aun no contiene hashes modernos.
- WPF sigue usando login directo contra SQL.
- Falta validar migracion controlada de un unico usuario hacia BCrypt.

## Recomendacion para la siguiente fase

Ejecutar Fase 3B.4B-2: activar `EnableLegacyHashUpgrade=true` de forma temporal y controlada, repetir login manual con un unico usuario autorizado, confirmar que solo ese usuario recibe `password_hash_v2` BCrypt y `password_migrada_utc`, y luego volver a dejar `EnableLegacyHashUpgrade=false` hasta completar el plan de despliegue.
