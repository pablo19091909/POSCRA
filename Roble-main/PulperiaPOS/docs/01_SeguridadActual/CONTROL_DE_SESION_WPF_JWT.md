# Control de sesion WPF con JWT

## Estado implementado

WPF puede guardar informacion de autenticacion API en `UserSession` solo en memoria:

- `IsApiAuthenticated`
- `AccessToken`
- `TokenExpiresAtUtc`
- `Permissions`

Tambien conserva compatibilidad con campos existentes:

- `IdUsuario`
- `NombreUsuario`
- `RolUsuario`

## Alcance de seguridad

El JWT:

- se recibe desde `POST /api/auth/login`;
- se mantiene solamente en memoria;
- no se escribe en appsettings;
- no se escribe en logs;
- no se escribe en base de datos;
- no se muestra completo al usuario.

## Limpieza de sesion

`UserSession.Clear()` limpia:

- id de usuario;
- nombre;
- rol;
- indicador API;
- access token;
- expiracion;
- permisos.

`VentanaAdministrador` y `VentanaAnfitrion` llaman a `UserSession.Clear()` durante logout.

## Compatibilidad temporal

Las pantallas existentes pueden seguir leyendo:

- `UserSession.IdUsuario`
- `UserSession.NombreUsuario`
- `UserSession.RolUsuario`

Los permisos de API quedan almacenados para fases futuras, pero aun no reemplazan todas las validaciones WPF.

## Riesgos pendientes

- El token no se refresca automaticamente.
- No hay expiracion visual de sesion en WPF.
- Los modulos funcionales siguen usando SQL directo.
- La autorizacion por permisos aun no controla todas las pantallas WPF.

## Recomendacion

En la siguiente fase, agregar validacion centralizada de expiracion del token y preparar un servicio WPF para llamadas API autenticadas, sin migrar todavia ventas, caja, inventario, clientes ni reportes.
