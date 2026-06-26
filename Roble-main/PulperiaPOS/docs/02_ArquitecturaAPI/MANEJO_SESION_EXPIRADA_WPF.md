# Manejo de sesion expirada en WPF

## Estrategia elegida

La aplicacion usa un coordinador central:

- `ApiSessionCoordinator`
- `ApiSessionNavigationCoordinator`

`ApiClientBase` notifica al coordinador cuando detecta:

- token ausente en una llamada autenticada;
- token localmente vencido;
- HTTP 401 desde `POS.Api`.

## Flujo

1. Un cliente futuro llama `SendAsync<T>(..., requiresAuthentication: true)`.
2. `ApiClientBase` valida `UserSession`.
3. Si la sesion no es valida, no envia la peticion autenticada.
4. Se ejecuta `UserSession.Clear()`.
5. Se dispara un evento central de sesion expirada.
6. La UI muestra un unico mensaje seguro.
7. Se abre `LoginWindow` si no existe uno visible.
8. Se cierran ventanas abiertas que no son `LoginWindow`.

## Evitar mensajes repetidos

`ApiSessionCoordinator` evita notificaciones duplicadas con una bandera interna.

`LoginWindow` llama `ApiSessionCoordinator.Reset()` despues de un login correcto, sea SQL o API.

## Compatibilidad con login SQL

Este mecanismo no elimina el login SQL.

Si `UseApiLogin=false`, el login SQL tradicional sigue disponible.

No existe fallback automatico API -> SQL cuando una llamada autenticada falla. Esto evita que una autorizacion vencida o invalida ejecute una operacion por otro canal.

## Seguridad

Al expirar sesion se limpia:

- id de usuario;
- nombre;
- rol;
- indicador de autenticacion API;
- token;
- expiracion;
- permisos.

No se imprime ni registra el JWT.

## Como probar sin revelar tokens

Pruebas seguras:

- llamar un endpoint protegido sin token y confirmar HTTP 401;
- configurar temporalmente una sesion en memoria con expiracion pasada en entorno de desarrollo;
- confirmar que no se envia la peticion autenticada;
- confirmar que `UserSession.Clear()` deja token y permisos vacios.

No se debe copiar ni pegar un token real en documentos, tickets, logs ni consola compartida.

## Limitaciones actuales

- No hay refresh token.
- No hay renovacion automatica.
- Las pantallas existentes aun no usan clientes API de negocio.
- El mecanismo se aplicara gradualmente conforme se migren modulos.
