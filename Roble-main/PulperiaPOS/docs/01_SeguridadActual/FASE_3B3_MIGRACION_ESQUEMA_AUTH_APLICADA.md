# Fase 3B.3 - Migracion de esquema Auth aplicada

## Fecha y hora UTC

Aplicacion y validacion: `2026-06-25T02:49:16Z`.

## Script ejecutado

Se ejecuto una sola vez:

- `database/migrations/006_AuthModernizacionUsuarios.sql`

No se ejecuto rollback.

## Respaldo o recuperacion

El contexto operativo de la fase confirma que existe respaldo o mecanismo de recuperacion validado por el operador antes de ejecutar.

## Validaciones previas

| Validacion | Resultado |
| --- | --- |
| Script revisado y aprobado | Correcto |
| Sin `DROP` | Correcto |
| Sin `DELETE` | Correcto |
| Sin `UPDATE` sobre `contrasena` | Correcto |
| Sin reduccion de tamanos | Correcto |
| Sin renombre/eliminacion de columnas existentes | Correcto |
| Sin cambios a FK existentes | Correcto |
| `EnableLegacyHashUpgrade=false` | Correcto |
| `/health/database` previo | HTTP 200 |

## Diagnostico previo agregado

| Metrica | Antes |
| --- | --- |
| Usuarios totales | 12 |
| Usuarios con password heredado | 12 |
| Hashes modernos existentes | 0 |
| Grupos de nombres duplicados | 0 |
| Usuarios en grupos duplicados | 0 |
| Contrasenas nulas/vacias | 0 |
| Nombres nulos/vacios | 0 |
| Roles nulos/invalidos | 0 |

No se mostraron nombres de usuario, contrasenas ni hashes.

## Resultado de ejecucion

La migracion finalizo correctamente dentro del script transaccional.

Resultado: `EXECUTED`.

Error: ninguno.

## Columnas creadas y tipos verificados

| Columna | Tipo verificado | Nullable | Estado |
| --- | --- | --- | --- |
| `password_hash_v2` | `nvarchar(255)` | Si | Creada |
| `password_hash_version` | `nvarchar(50)` | Si | Creada |
| `activo` | `bit` | No | Creada |
| `intentos_fallidos` | `int` | No | Creada |
| `bloqueado_hasta_utc` | `datetime2(0)` | Si | Creada |
| `ultimo_login_utc` | `datetime2(0)` | Si | Creada |
| `password_migrada_utc` | `datetime2(0)` | Si | Creada |
| `creado_utc` | `datetime2(0)` | Si | Creada |
| `actualizado_utc` | `datetime2(0)` | Si | Creada |

Tambien se verifico:

- `DF_usuario_activo` presente.
- `DF_usuario_intentos_fallidos` presente.
- `IX_usuario_nombre_auth` presente.

## Resultado agregado despues de migracion

| Metrica | Despues |
| --- | --- |
| Usuarios totales | 12 |
| `password_hash_v2` con valor | 0 |
| `password_hash_v2` NULL | 12 |
| `password_hash_version` NULL | 12 |
| Usuarios activos | 12 |
| Usuarios con `intentos_fallidos = 0` | 12 |
| Password heredado presente | 12 |
| Nombres nulos/vacios | 0 |
| Roles nulos/vacios | 0 |

## Confirmacion de password heredado

La migracion no contiene instrucciones para modificar `usuario.contrasena`.

Validacion posterior:

- Los 12 usuarios conservan password heredado presente.
- No se escribio ningun hash moderno.
- No se genero BCrypt.
- `EnableLegacyHashUpgrade` permanecio en `false`.

## Dependencias y FK

| Dependencia | Resultado |
| --- | --- |
| `ventas.usuario_id -> usuario.idUsuario` | Sigue presente |
| POS.Api `/health/database` | HTTP 200 |
| WPF | Compila correctamente |

No se modificaron ventas, caja, inventario, clientes, cierres, reportes ni tablas relacionadas.

## Validacion del sistema

| Validacion | Resultado |
| --- | --- |
| Compilacion solucion completa | Correcta |
| `PulperiaPOS` | Compila |
| `POS.Api` | Compila |
| `/health` | HTTP 200 |
| `/health/database` | HTTP 200 |
| `/api/system/version` | HTTP 200 |

No se hizo login exitoso por API.

## Riesgos pendientes

- WPF sigue usando login directo con SHA-256 legado.
- Todavia no se han migrado hashes a BCrypt.
- `EnableLegacyHashUpgrade` sigue desactivado.
- `rol` sigue permitiendo NULL a nivel de esquema.
- Caja y otros movimientos mantienen auditoria de usuario incompleta.

## Recomendacion para Fase 3B.4

Ejecutar una validacion controlada de autenticacion API con un usuario de prueba:

1. Configurar JWT local seguro sin exponer secretos.
2. Mantener `EnableLegacyHashUpgrade=false` inicialmente.
3. Probar `POST /api/auth/login` solo con usuario de prueba.
4. Confirmar que el login legado funciona sin escribir `password_hash_v2`.
5. Activar `EnableLegacyHashUpgrade=true` solo para prueba controlada.
6. Confirmar migracion a BCrypt solo del usuario de prueba.
7. Mantener WPF sin cambios funcionales hasta validar el flujo completo.

## Confirmaciones finales

- No se modifico `usuario.contrasena`.
- No se cambiaron contrasenas.
- No se generaron hashes BCrypt.
- No se modifico WPF funcionalmente.
- No se modificaron ventas, caja, inventario, clientes ni reportes.
- No se revelaron secretos.
