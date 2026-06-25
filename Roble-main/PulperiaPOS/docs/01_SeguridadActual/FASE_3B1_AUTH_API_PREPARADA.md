# Fase 3B.1 - Auth API preparada

## Alcance implementado

Se preparo `POS.Api` para autenticacion segura compatible con usuarios existentes que aun tienen hash SHA-256 legado.

No se modifico WPF, no se cambio el flujo actual de login WPF, no se ejecuto SQL y no se actualizaron usuarios reales.

## Paquetes agregados

- `BCrypt.Net-Next` para crear y validar hashes BCrypt.
- `Microsoft.AspNetCore.Authentication.JwtBearer` para preparar autenticacion JWT.

## Componentes creados

| Componente | Responsabilidad |
| --- | --- |
| `IAuthService` / `AuthService` | Orquestar login, validar password, resolver permisos y emitir token. |
| `IUserRepository` / `UserRepository` | Leer usuario desde SQL Server con consultas parametrizadas y conexion centralizada. |
| `IPasswordHasher` / `BcryptPasswordHasher` | Crear/verificar hashes BCrypt. |
| `ILegacyPasswordVerifier` / `LegacySha256PasswordVerifier` | Verificar hashes SHA-256 legados compatibles con WPF. |
| `ITokenService` / `JwtTokenService` | Emitir JWT cuando existe configuracion segura. |
| `RolePermissionProvider` | Resolver permisos iniciales desde roles existentes. |
| `AuthController` | Exponer `POST /api/auth/login`. |

## Comportamiento antes de migracion SQL

Antes de ejecutar la migracion, la tabla `usuario` solo tiene `idUsuario`, `nombre`, `contrasena` y `rol`.

El endpoint esta preparado para:

- consultar usuario por nombre;
- validar SHA-256 legado si no existe `password_hash_v2`;
- no actualizar hashes si `Authentication:EnableLegacyHashUpgrade=false`;
- devolver error seguro si credenciales son invalidas;
- emitir JWT solo si existe clave JWT real y segura.

Las columnas modernas se detectan dinamicamente. Si no existen, el endpoint evita referenciarlas en la consulta principal.

## Comportamiento despues de migracion SQL

Cuando existan las columnas modernas:

- si `password_hash_v2` tiene valor, se valida con BCrypt;
- si `password_hash_v2` esta vacio, se valida SHA-256 legado;
- si `EnableLegacyHashUpgrade=true` y el login legado es correcto, se podra escribir `password_hash_v2`, `password_hash_version` y `password_migrada_utc`;
- usuarios con `activo = 0` no podran iniciar sesion;
- usuarios con `bloqueado_hasta_utc` futuro no podran iniciar sesion.

En esta fase el valor por defecto de `EnableLegacyHashUpgrade` es `false`.

## Permisos iniciales

`Administrador` recibe la matriz completa definida para esta etapa.

`Anfitrion` recibe solo permisos operativos actuales:

- `Ventas.Crear`
- `Ventas.Ver`
- `Inventario.Ver`
- `Clientes.Ver`
- `Caja.Cerrar`
- `Caja.VerResumen`
- `TipoCambio.Ver`

## Rate limiting

Se implemento rate limiting solo para `POST /api/auth/login`.

Configuracion por defecto:

- 5 solicitudes;
- ventana de 60 segundos;
- respuesta 429 cuando se excede;
- no afecta `/health`, `/health/database` ni `/api/system/version`.

## Validaciones ejecutadas

| Validacion | Resultado |
| --- | --- |
| Compilacion solucion completa | Correcta |
| WPF compila | Correcta |
| POS.Api compila | Correcta |
| `/health` | 200 |
| `/api/system/version` | 200 |
| `/health/database` sin conexion temporal | 503 seguro |
| `/health/database` con conexion temporal local | 200 |
| Swagger/OpenAPI sin terminos sensibles de configuracion | Correcto |
| `/api/auth/login` con request invalido | 400 seguro |
| Rate limit login | 429 tras superar limite |
| `/health` luego de rate limit | 200 |
| Auth habilitada sin JWT signing key real | Falla segura en startup |

No se ejecuto login exitoso contra usuarios reales.

## Pruebas automaticas o estrategia documentada

En esta fase se ejecutaron pruebas manuales seguras sobre endpoints publicos y solicitud invalida de login.

Estrategia recomendada para pruebas automatizadas de Fase 3B.2:

- solicitud vacia;
- usuario inexistente;
- contrasena incorrecta;
- usuario inactivo con columnas modernas;
- SHA-256 legado valido;
- SHA-256 legado invalido;
- BCrypt valido;
- BCrypt invalido;
- token valido;
- token vencido;
- token manipulado;
- rate limit;
- health publico;
- secreto JWT ausente;
- `EnableLegacyHashUpgrade=false`.

Estas pruebas deben usar base aislada, mocks o datos de prueba, nunca usuarios reales.

## Confirmaciones

- No se modifico WPF funcionalmente.
- No se modifico `DBConnection.cs`.
- No se modifico la base de datos.
- No se ejecutaron scripts SQL.
- No se cambiaron hashes ni contrasenas.
- No se agregaron secretos al repositorio.
- No se agregaron endpoints de ventas, inventario, clientes, caja ni reportes.
- No se crearon refresh tokens.
