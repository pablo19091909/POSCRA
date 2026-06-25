# Plan de migracion de SHA-256 a hash moderno

## Estado actual

La WPF usa SHA-256 simple para validar contrasenas contra la tabla `usuario`.

Caracteristicas actuales:

- sin sal;
- sin factor de costo;
- sin version de algoritmo;
- hash generado en cliente;
- validacion directa contra Azure SQL desde WPF.

## Objetivo futuro

Migrar a un hash adaptativo moderno sin interrumpir usuarios existentes.

Algoritmos recomendados:

1. Argon2id, si la dependencia y despliegue son aceptables.
2. BCrypt, si se prefiere compatibilidad amplia y operacion simple en .NET.

Para la siguiente fase se recomienda BCrypt como primera implementacion practica, con posibilidad de evolucionar a Argon2id.

## Estrategia de migracion progresiva

1. El usuario intenta iniciar sesion en `POS.Api`.
2. La API busca usuario por nombre.
3. Si el hash almacenado es moderno, valida con BCrypt/Argon2.
4. Si el hash almacenado es legado SHA-256, calcula SHA-256 de la contrasena ingresada y compara.
5. Si la validacion legado es exitosa, recalcula hash moderno.
6. La API actualiza el hash y marca el algoritmo moderno.
7. El usuario entra sin interrupcion.

Nunca se debe guardar ni registrar contrasena en texto plano.

## Columnas nuevas recomendadas

No se crean en esta fase. Para implementar la migracion se recomienda agregar en fase futura:

| Columna | Proposito |
| --- | --- |
| `password_hash` o reutilizar `contrasena` | Almacenar hash moderno. |
| `password_hash_algorithm` | Distinguir `sha256-legacy`, `bcrypt`, `argon2id`. |
| `password_updated_at` | Auditoria de migracion. |
| `is_active` | Bloquear acceso sin eliminar usuario. |
| `failed_login_count` | Soporte a bloqueo/rate limiting. |
| `locked_until` | Bloqueo temporal. |
| `last_login_at` | Auditoria. |
| `created_at`, `updated_at` | Auditoria. |

Alternativa minima:

- reutilizar `contrasena`;
- distinguir por formato del hash:
  - SHA-256 legado: 64 caracteres hexadecimales;
  - BCrypt: prefijo `$2a$`, `$2b$` o `$2y$`;
  - Argon2id: prefijo `$argon2id$`.

La alternativa por formato evita columna inmediata, pero es menos explicita que una columna de algoritmo.

## Usuarios que nunca vuelven a iniciar sesion

Los usuarios que no vuelvan a iniciar sesion conservaran hash legado hasta que:

- inicien sesion exitosamente;
- un administrador fuerce cambio de contrasena;
- se ejecute una campana controlada de rotacion;
- se desactiven cuentas antiguas tras una politica definida.

## Rollback

Plan de rollback recomendado:

1. Mantener compatibilidad de lectura con SHA-256 durante una ventana definida.
2. No eliminar soporte legado hasta confirmar migracion de cuentas activas.
3. Registrar solo eventos tecnicos seguros, nunca hashes ni contrasenas.
4. Si falla la implementacion moderna, permitir temporalmente login legado sin rehash.
5. No revertir hashes ya migrados a SHA-256.

## Riesgos de compatibilidad

| Riesgo | Mitigacion |
| --- | --- |
| Hashes existentes con formatos inesperados | Clasificar como invalido y pedir reset seguro. |
| Usuarios editados desde WPF durante transicion | Congelar administracion WPF o enrutar usuarios por API antes de migrar. |
| Rehash doble por editar usuario | Evitar que WPF escriba contrasenas cuando API controle autenticacion. |
| Falta de columna `is_active` | No se puede invalidar usuario sin borrado fisico. |
| Algoritmo moderno con costo alto | Medir tiempos y ajustar factor de costo. |

## Reglas de logging

- No registrar contrasenas.
- No registrar hashes.
- No registrar cadenas de conexion.
- Registrar solo `userId`, resultado seguro, `traceId`, tipo de error y timestamp UTC.

## Recomendacion para implementacion

En Fase 3B implementar:

- `IPasswordHasher`;
- `LegacySha256PasswordVerifier`;
- `ModernPasswordHasher`;
- `PasswordHashUpgradeService`;
- endpoint de login en API;
- pruebas unitarias para hashes legado y moderno;
- rate limiting de login.
