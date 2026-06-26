# Fase 3C.1 - Prueba operativa Login WPF mediante POS.Api

Fecha UTC: 2026-06-26 01:23:26 UTC

## Objetivo

Validar operativamente que WPF puede autenticar mediante `POS.Api` con:

- un usuario SHA-256 legado;
- un usuario BCrypt;
- fallback temporal al login SQL tradicional.

No se documentaron usuarios, contrasenas, tokens, hashes ni secretos.

## API disponible

| Validacion | Resultado |
|---|---|
| `/health` | HTTP 200 |
| `/health/database` | HTTP 200 |
| `/api/system/version` | HTTP 200 |
| API por HTTPS | Confirmado |
| `Authentication:EnableLegacyHashUpgrade` | `false` |

## Resultado login API SHA-256

El operador confirmo manualmente:

- login WPF por API correcto con cuenta SHA-256 legado;
- apertura de ventana correcta segun rol;
- sin fallback silencioso al login SQL;
- token mantenido solo en memoria;
- logout correcto.

No se revelaron credenciales ni token.

## Resultado login API BCrypt

El operador confirmo manualmente:

- login WPF por API correcto con cuenta BCrypt;
- rol disponible en sesion;
- permisos disponibles en sesion para uso futuro;
- apertura de ventana correcta segun rol;
- logout correcto;
- no se modifico otro hash.

No se revelaron credenciales ni token.

## Resultado fallback SQL

El operador confirmo manualmente:

- `UseApiLogin=false` funciona con login SQL tradicional;
- WPF no usa API cuando el flag esta en `false`;
- el login API puede reactivarse luego sin cambio de codigo.

Al cierre de la validacion, la configuracion local WPF revisada tenia `UseApiLogin=false`.

## Resultado logout

El logout fue confirmado por el operador en ambos modos probados.

`UserSession.Clear()` limpia:

- id de usuario;
- nombre;
- rol;
- indicador de autenticacion API;
- access token;
- expiracion;
- permisos.

## Integridad posterior

Validacion realizada solo con SELECT agregados:

| Metrica | Resultado |
|---|---:|
| Usuarios totales | 12 |
| Hashes BCrypt | 1 |
| Hashes SHA-256 legados presentes en `usuario.contrasena` | 12 |
| Usuarios activos | 12 |
| Roles distintos | 2 |

No se listaron usuarios, roles individuales, hashes ni contrasenas.

## Compilacion

| Proyecto | Resultado |
|---|---|
| POS.Api | Compila sin errores |
| PulperiaPOS WPF | Compila sin errores |
| Solucion completa | Compila sin errores |

Comando ejecutado:

```powershell
dotnet build .\PulperiaPOS.sln /p:UseAppHost=false /p:OutDir=obj\CodexBuild3C1\
```

Resultado: 0 advertencias, 0 errores.

## Confirmaciones de seguridad

- No se modifico codigo durante esta fase.
- No se modifico base de datos.
- No se ejecutaron scripts SQL.
- No se ejecutaron migraciones.
- No se activó `Authentication:EnableLegacyHashUpgrade`.
- No se migraron usuarios adicionales.
- No se modificaron hashes ni contrasenas.
- No se realizaron ventas, movimientos de caja, inventario, clientes, donaciones ni reportes.
- No se guardaron tokens en disco.
- No se documentaron secretos.

## Riesgos pendientes

- Los modulos funcionales WPF siguen usando SQL directo.
- El JWT aun no se usa para autorizar operaciones de negocio desde WPF.
- No existe expiracion visual de sesion ni renovacion de token en WPF.
- `VentanaUsuarios` aun administra contrasenas contra SQL directo.

## Recomendacion

Ejecutar la Fase 3D: introducir un servicio WPF centralizado para llamadas API autenticadas y validacion de expiracion de token, manteniendo SQL directo en modulos de negocio hasta migrarlos uno por uno.
