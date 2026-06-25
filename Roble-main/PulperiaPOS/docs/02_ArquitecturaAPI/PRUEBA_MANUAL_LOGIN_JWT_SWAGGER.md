# Prueba manual de login JWT en Swagger

## Objetivo

Validar manualmente `POST /api/auth/login` con una cuenta autorizada sin revelar credenciales ni token y sin migrar hashes.

## Reglas

- No compartir usuario, contrasena ni token.
- No pegar el token en documentos o chats.
- Mantener `Authentication:EnableLegacyHashUpgrade=false`.
- No usar endpoints de negocio.
- Retirar el token de Swagger al terminar.

## Iniciar POS.Api

Desde la carpeta de la solucion:

```powershell
dotnet run --project .\POS.Api\POS.Api.csproj
```

Abrir Swagger en la URL local indicada por la consola, por ejemplo:

```text
http://localhost:PUERTO/swagger
```

## Ejecutar login

1. Abrir `POST /api/auth/login`.
2. Presionar `Try it out`.
3. Ingresar credenciales reales solamente en el navegador local del operador.
4. Ejecutar la solicitud.
5. Confirmar HTTP 200.

La respuesta debe incluir:

- `accessToken`;
- `expiresAtUtc`;
- `user.id`;
- `user.username`;
- `user.role`;
- `user.permissions`.

La respuesta no debe incluir:

- contrasena;
- hashes;
- connection strings;
- signing key;
- datos innecesarios.

## Autorizar Swagger

1. Copiar temporalmente solo el valor de `accessToken`.
2. Presionar `Authorize`.
3. Escribir:

```text
Bearer TOKEN_TEMPORAL
```

4. Confirmar.

No guardar el token.

## Probar token

Ejecutar:

```text
GET /api/auth/me
```

Resultado esperado:

- HTTP 200;
- userId;
- username;
- role;
- permissions.

Ejecutar una prueba de permiso:

```text
GET /api/auth/permission-test/Ventas.Ver
```

Resultado esperado si el token tiene permiso:

- HTTP 200.

Ejecutar una prueba con permiso no otorgado:

```text
GET /api/auth/permission-test/Usuarios.Administrar
```

Resultado esperado para usuario sin ese permiso:

- HTTP 403.

## Retirar token

Al terminar:

1. Presionar `Authorize`.
2. Presionar `Logout`.
3. Cerrar Swagger o recargar la pagina.

## Verificar que no cambio ningun hash

Ejecutar solo consultas agregadas de lectura:

```sql
SELECT
    COUNT(*) AS total_usuarios,
    SUM(CASE WHEN password_hash_v2 IS NULL THEN 1 ELSE 0 END) AS hash_v2_null,
    SUM(CASE WHEN password_hash_v2 IS NOT NULL THEN 1 ELSE 0 END) AS hash_v2_con_valor,
    SUM(CASE WHEN password_hash_version IS NULL THEN 1 ELSE 0 END) AS version_null,
    SUM(CASE WHEN password_migrada_utc IS NULL THEN 1 ELSE 0 END) AS migrada_null,
    SUM(CASE WHEN ultimo_login_utc IS NULL THEN 1 ELSE 0 END) AS ultimo_login_null
FROM dbo.usuario;
```

Resultado esperado en Fase 3B.4A:

- 12 usuarios;
- 12 `password_hash_v2` NULL;
- 0 `password_hash_v2` con valor;
- 12 `password_hash_version` NULL;
- 12 `password_migrada_utc` NULL;
- 12 `ultimo_login_utc` NULL.

No mostrar nombres de usuario, hashes ni contrasenas.
