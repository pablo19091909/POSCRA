# Diseno JWT y autorizacion para POS.Api

## Objetivo

Definir el diseno futuro de autenticacion y autorizacion para `POS.Api` sin implementarlo en esta fase.

## Flujo propuesto de login

1. WPF envia usuario y contrasena a `POST /api/auth/login`.
2. POS.Api valida credenciales con servicio de contrasenas.
3. POS.Api verifica que el usuario este activo cuando exista el campo.
4. POS.Api resuelve rol y permisos.
5. POS.Api emite access token.
6. WPF almacena el token solo en memoria durante la sesion inicial.
7. WPF llama endpoints con `Authorization: Bearer`.

## Claims minimos

| Claim | Uso |
| --- | --- |
| `userId` | Identificador interno del usuario. |
| `username` | Nombre de usuario para auditoria y UI. |
| `role` | Rol base actual o futuro. |
| `permissions` | Lista de permisos otorgados. |

Claims adicionales recomendados:

- `jti`: identificador unico del token.
- `iat`: emitido en.
- `exp`: expiracion.
- `tokenVersion`: invalida tokens tras cambios criticos.

## Duracion de access token

Recomendacion inicial:

- 30 a 60 minutos para access token.
- Renovacion por nuevo login manual en la primera etapa.

Evitar tokens excesivamente largos porque la WPF operara en estaciones compartidas.

## Refresh tokens

No se recomienda implementar refresh tokens en la primera etapa.

Motivo:

- aumenta complejidad de almacenamiento seguro;
- requiere revocacion;
- requiere tabla o store persistente;
- la prioridad inicial es login seguro y autorizacion server-side.

Cuando se implementen, deben ser rotativos, revocables y almacenados hasheados.

## Invalidacion de usuarios desactivados

Cuando exista `is_active`:

- login debe rechazar usuarios inactivos;
- cada endpoint sensible puede validar token version o estado activo server-side;
- cambios de rol/permisos deben invalidar tokens emitidos con version anterior.

Opciones:

1. Token corto sin validacion por request de estado activo.
2. Token corto + `tokenVersion`.
3. Token corto + cache server-side de usuarios desactivados.

Recomendacion: access token corto + `tokenVersion` cuando se agregue columna.

## Validacion de permisos

Cada endpoint debe declarar permiso requerido:

- `Ventas.Crear`
- `Inventario.Editar`
- `Caja.Retirar`
- etc.

La API debe:

1. autenticar token;
2. validar firma, emisor, audiencia y expiracion;
3. extraer permisos;
4. comprobar permiso requerido;
5. devolver 403 si falta permiso.

WPF no debe ser fuente de verdad de autorizacion.

## Errores esperados

| Caso | Respuesta |
| --- | --- |
| Sin token | 401 |
| Token vencido | 401 con mensaje seguro de sesion expirada. |
| Token manipulado | 401 sin detalle tecnico. |
| Usuario sin permiso | 403 con mensaje seguro. |
| Usuario inactivo | 401 o 403 segun politica definida. |
| Error interno | 500 con `{ traceId, message }`. |

## Rate limiting de login

Recomendacion inicial:

- limite por IP/equipo;
- limite por usuario;
- backoff incremental;
- bloqueo temporal tras intentos fallidos;
- logs seguros sin contrasenas ni hashes.

No bloquear cuentas permanentemente sin proceso administrativo.

## Configuracion segura

JWT requiere secretos/configuracion fuera del repositorio:

- clave de firma;
- issuer;
- audience;
- expiracion;
- clock skew.

No se deben almacenar en `appsettings.json` valores reales.

## Politicas recomendadas en ASP.NET Core

Politicas futuras:

- `RequirePermission("Usuarios.Administrar")`
- `RequirePermission("Ventas.Crear")`
- `RequirePermission("Caja.Retirar")`
- `RequirePermission("Configuracion.Administrar")`

La implementacion puede usar un `AuthorizationHandler` que lea permisos desde claims.

## Recomendacion para Fase 3B

Implementar autenticacion minima en `POS.Api`:

1. Servicio de login.
2. Verificacion SHA-256 legado + hash moderno.
3. Emision de JWT.
4. Middleware de autenticacion.
5. Politicas por permiso.
6. Endpoint `POST /api/auth/login`.
7. Endpoint seguro de prueba, por ejemplo `GET /api/auth/me`.
8. Pruebas sin modificar modulos funcionales.
