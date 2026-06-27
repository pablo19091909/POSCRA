# Permisos ventas API

## Permiso usado

`Ventas.Crear`

El permiso ya existia en la matriz tecnica de POS.Api y no fue necesario crear uno nuevo.

## Roles actuales

| Rol | `Ventas.Crear` |
| --- | --- |
| Administrador | Si |
| Anfitrion | Si |

No se reutiliza `Ventas.Ver` para registrar ventas.

## Reglas del endpoint

| Caso | Resultado esperado |
| --- | --- |
| Sin token | HTTP 401 |
| Token sin `Ventas.Crear` | HTTP 403 |
| Token con `Ventas.Crear` y flag apagado | HTTP 503 seguro |
| Token con `Ventas.Crear` y flag futuro encendido | Ejecuta validaciones antes de escritura |

## Seguridad

WPF no es autoridad de permisos. La autorizacion valida es JWT + claims emitidos por POS.Api.

No se guardan tokens en logs ni en documentacion.
