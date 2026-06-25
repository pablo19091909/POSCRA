/*
    Diagnostico Fase 3B.1 - Usuarios y autenticacion.

    Solo consultas SELECT.
    No muestra contrasenas, hashes ni secretos.
*/

IF OBJECT_ID(N'dbo.usuario', N'U') IS NULL
BEGIN
    SELECT 'dbo.usuario' AS objeto, 'NO_EXISTE' AS estado;
    RETURN;
END;

SELECT 'total_usuarios' AS metrica, COUNT(*) AS valor
FROM dbo.usuario;

SELECT 'usuarios_con_password_heredado' AS metrica, COUNT(*) AS valor
FROM dbo.usuario
WHERE contrasena IS NOT NULL;

SELECT
    'columna_password_hash_v2_existe' AS metrica,
    CASE WHEN COL_LENGTH(N'dbo.usuario', N'password_hash_v2') IS NULL THEN 0 ELSE 1 END AS valor;

SELECT 'usuarios_sin_rol' AS metrica, COUNT(*) AS valor
FROM dbo.usuario
WHERE rol IS NULL OR LTRIM(RTRIM(rol)) = '';

SELECT 'usuarios_sin_nombre' AS metrica, COUNT(*) AS valor
FROM dbo.usuario
WHERE nombre IS NULL OR LTRIM(RTRIM(nombre)) = '';

SELECT 'grupos_nombre_duplicado' AS metrica, COUNT(*) AS valor
FROM (
    SELECT nombre
    FROM dbo.usuario
    WHERE nombre IS NOT NULL
    GROUP BY nombre
    HAVING COUNT(*) > 1
) duplicados;

SELECT 'usuarios_en_grupos_nombre_duplicado' AS metrica, ISNULL(SUM(total), 0) AS valor
FROM (
    SELECT COUNT(*) AS total
    FROM dbo.usuario
    WHERE nombre IS NOT NULL
    GROUP BY nombre
    HAVING COUNT(*) > 1
) duplicados;

SELECT 'usuarios_con_contrasena_nula' AS metrica, COUNT(*) AS valor
FROM dbo.usuario
WHERE contrasena IS NULL OR LTRIM(RTRIM(contrasena)) = '';

SELECT 'usuarios_con_rol_no_reconocido' AS metrica, COUNT(*) AS valor
FROM dbo.usuario
WHERE rol IS NOT NULL
  AND LTRIM(RTRIM(rol)) <> ''
  AND rol NOT IN (N'Administrador', N'Anfitrion');

SELECT
    c.name AS columna,
    t.name AS tipo,
    c.max_length,
    c.is_nullable
FROM sys.columns c
JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID(N'dbo.usuario')
ORDER BY c.column_id;

SELECT 'password_hash_v2' AS columna, CASE WHEN COL_LENGTH(N'dbo.usuario', N'password_hash_v2') IS NULL THEN 'FALTA' ELSE 'EXISTE' END AS estado
UNION ALL SELECT 'password_hash_version', CASE WHEN COL_LENGTH(N'dbo.usuario', N'password_hash_version') IS NULL THEN 'FALTA' ELSE 'EXISTE' END
UNION ALL SELECT 'activo', CASE WHEN COL_LENGTH(N'dbo.usuario', N'activo') IS NULL THEN 'FALTA' ELSE 'EXISTE' END
UNION ALL SELECT 'intentos_fallidos', CASE WHEN COL_LENGTH(N'dbo.usuario', N'intentos_fallidos') IS NULL THEN 'FALTA' ELSE 'EXISTE' END
UNION ALL SELECT 'bloqueado_hasta_utc', CASE WHEN COL_LENGTH(N'dbo.usuario', N'bloqueado_hasta_utc') IS NULL THEN 'FALTA' ELSE 'EXISTE' END
UNION ALL SELECT 'ultimo_login_utc', CASE WHEN COL_LENGTH(N'dbo.usuario', N'ultimo_login_utc') IS NULL THEN 'FALTA' ELSE 'EXISTE' END
UNION ALL SELECT 'password_migrada_utc', CASE WHEN COL_LENGTH(N'dbo.usuario', N'password_migrada_utc') IS NULL THEN 'FALTA' ELSE 'EXISTE' END
UNION ALL SELECT 'creado_utc', CASE WHEN COL_LENGTH(N'dbo.usuario', N'creado_utc') IS NULL THEN 'FALTA' ELSE 'EXISTE' END
UNION ALL SELECT 'actualizado_utc', CASE WHEN COL_LENGTH(N'dbo.usuario', N'actualizado_utc') IS NULL THEN 'FALTA' ELSE 'EXISTE' END;
