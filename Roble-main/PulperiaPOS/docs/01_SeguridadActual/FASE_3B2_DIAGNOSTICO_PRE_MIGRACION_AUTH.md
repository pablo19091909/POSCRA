# Fase 3B.2 - Diagnostico pre migracion Auth

## Fecha y hora UTC

Revision ejecutada: `2026-06-25T02:41:36Z`.

## Scripts revisados

- `database/diagnostics/006_DiagnosticoUsuariosAuth.sql`
- `database/migrations/006_AuthModernizacionUsuarios.sql`
- `database/rollback/006_AuthModernizacionUsuarios_rollback.sql`

## Confirmacion de ejecucion

Se ejecuto unicamente diagnostico de lectura y consultas de metadatos.

No se ejecutaron:

- `database/migrations/006_AuthModernizacionUsuarios.sql`
- `database/rollback/006_AuthModernizacionUsuarios_rollback.sql`
- `INSERT`
- `UPDATE`
- `DELETE`
- `ALTER`
- `CREATE`
- `DROP`
- `MERGE`
- `TRUNCATE`

El script de diagnostico fue endurecido para no devolver nombres de usuario individuales en la validacion de duplicados. Ahora reporta solo conteos agregados.

## Estado actual de `usuario`

| Campo | Tipo | Longitud reportada | Nullable |
| --- | --- | --- | --- |
| `idUsuario` | `int` | 4 | No |
| `nombre` | `nvarchar` | 200 | No |
| `contrasena` | `nvarchar` | 510 | No |
| `rol` | `nvarchar` | 100 | Si |

## Resultado agregado de usuarios

| Metrica | Resultado |
| --- | --- |
| Total de usuarios | 12 |
| Usuarios con password heredado | 12 |
| Usuarios con hash moderno | 0 |
| Usuarios sin nombre | 0 |
| Grupos de nombres duplicados | 0 |
| Usuarios dentro de grupos duplicados | 0 |
| Usuarios sin rol | 0 |
| Usuarios con rol no reconocido | 0 |
| Usuarios con contrasena nula/vacia | 0 |

No se mostraron nombres de usuario, contrasenas ni hashes.

## Columnas modernas requeridas

| Columna | Estado |
| --- | --- |
| `password_hash_v2` | Falta |
| `password_hash_version` | Falta |
| `activo` | Falta |
| `intentos_fallidos` | Falta |
| `bloqueado_hasta_utc` | Falta |
| `ultimo_login_utc` | Falta |
| `password_migrada_utc` | Falta |
| `creado_utc` | Falta |
| `actualizado_utc` | Falta |

Este estado es esperado antes de ejecutar la migracion 006.

## Indices, llaves y dependencias

| Elemento | Resultado |
| --- | --- |
| Primary key de `usuario` | Existe sobre `idUsuario` |
| Indices adicionales reportados | No se reportaron indices adicionales sobre `usuario` |
| Foreign keys hacia `usuario` | `ventas.usuario_id -> usuario.idUsuario` |
| Triggers sobre `usuario` | 0 |
| Dependencias SQL por `sys.sql_expression_dependencies` | 0 reportadas |

## Referencias funcionales relacionadas

| Area | Estado |
| --- | --- |
| Ventas | Tiene FK por `ventas.usuario_id` hacia `usuario.idUsuario`. |
| Ingreso caja | Tiene campo `usuario` como texto, no FK. |
| Retiro caja | No se detecto `usuario_id`. |
| Cierre caja | No se detecto `usuario_id`. |
| Saldo liberado | No se detecto `usuario_id`. |
| Tipo cambio | No se detecto `usuario_id`. |

Estas referencias no bloquean la migracion de autenticacion, pero mantienen deuda de auditoria.

## Validacion del script de migracion

`database/migrations/006_AuthModernizacionUsuarios.sql` fue revisado sin ejecutarse.

Resultado:

- Es aditivo.
- Es idempotente mediante `COL_LENGTH` y validacion de indice existente.
- No elimina ni renombra `contrasena`.
- No modifica hashes existentes.
- No cambia usuarios actuales.
- `activo` usa default `(1)`, compatible con usuarios actuales.
- `intentos_fallidos` usa default `(0)`, compatible con usuarios actuales.
- Columnas de fechas y bloqueo permiten `NULL`.
- `password_hash_v2 NVARCHAR(255)` soporta BCrypt.
- No crea indice unico sobre `nombre`.
- No introduce constraints que fallen por los datos actuales observados.
- No rompe la FK existente desde `ventas`.

## Validacion del rollback

`database/rollback/006_AuthModernizacionUsuarios_rollback.sql` fue revisado sin ejecutarse.

Resultado:

- Revierte columnas e indice agregados por la migracion.
- Contiene advertencia explicita.
- Detiene rollback si detecta `password_hash_v2` con datos.
- No debe ejecutarse si ya hay usuarios autenticando con hash moderno.

## Riesgos de compatibilidad

| Hallazgo | Clasificacion | Comentario |
| --- | --- | --- |
| Todas las columnas modernas faltan | Puede migrarse sin problema | Es el estado esperado antes de la migracion. |
| `rol` permite NULL | Requiere seguimiento posterior | Los datos actuales no tienen roles nulos, pero el esquema lo permite. |
| `nombre` no tiene indice unico | Requiere seguimiento posterior | No hay duplicados actuales; no bloquear porque la migracion no agrega unico. |
| `VentanaUsuarios` usa `SELECT *` | Puede migrarse sin problema | Lee columnas por nombre y no depende del numero exacto de columnas. |
| `LoginWindow` consulta columnas especificas | Puede migrarse sin problema | La migracion no elimina ni cambia columnas usadas. |
| `VentanaEditarUsuario` valida duplicado en cliente | Requiere seguimiento posterior | Debe moverse a API/server-side en fases futuras. |
| Caja no referencia usuario con FK fuerte | Requiere seguimiento posterior | No bloquea auth, pero afecta auditoria. |
| No hay hashes modernos actuales | Puede migrarse sin problema | Esperado antes de activar upgrade. |

## Bloqueos

No se encontraron bloqueos para ejecutar la migracion aditiva 006.

## Recomendacion

**Aprobado para migracion.**

## Pasos requeridos antes de ejecutar migracion

1. Realizar respaldo completo de Azure SQL.
2. Confirmar ventana de mantenimiento.
3. Confirmar que `EnableLegacyHashUpgrade=false`.
4. Ejecutar nuevamente `database/diagnostics/006_DiagnosticoUsuariosAuth.sql`.
5. Revisar que no aparezcan duplicados, roles invalidos ni usuarios sin contrasena.
6. Ejecutar manualmente `database/migrations/006_AuthModernizacionUsuarios.sql`.
7. Ejecutar nuevamente diagnostico.
8. Configurar JWT local seguro sin revelar secretos.
9. Probar login API solo con usuario de prueba.
10. Activar `EnableLegacyHashUpgrade=true` solo despues de validar la migracion.

## Validacion final

- Solucion completa compilo correctamente.
- `PulperiaPOS` compilo correctamente.
- `POS.Api` compilo correctamente.
- `/health/database` respondio HTTP 200.
- No se modifico WPF.
- No se modifico la base de datos.
- No se modificaron usuarios.
- No se modificaron hashes ni contrasenas.
- No se ejecuto migracion ni rollback.
