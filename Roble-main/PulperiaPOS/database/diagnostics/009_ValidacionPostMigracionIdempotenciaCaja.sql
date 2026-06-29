/*
    Validacion post migracion 009 - Idempotencia Caja API.
    SOLO LECTURA. Ejecutar solo despues de una aplicacion aprobada de 009.
*/

SET NOCOUNT ON;

SELECT
    'caja_idempotencia_exists' AS check_name,
    CASE WHEN OBJECT_ID(N'dbo.caja_idempotencia', N'U') IS NULL THEN 0 ELSE 1 END AS check_value;

SELECT
    c.column_id,
    c.name AS column_name,
    ty.name AS type_name,
    c.max_length,
    c.precision,
    c.scale,
    c.is_nullable
FROM sys.columns c
JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE c.object_id = OBJECT_ID(N'dbo.caja_idempotencia')
ORDER BY c.column_id;

SELECT
    i.name AS index_name,
    i.is_unique,
    i.has_filter,
    i.filter_definition
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID(N'dbo.caja_idempotencia')
  AND i.name IS NOT NULL
ORDER BY i.name;

SELECT
    fk.name AS foreign_key_name,
    OBJECT_NAME(fk.parent_object_id) AS parent_table,
    OBJECT_NAME(fk.referenced_object_id) AS referenced_table
FROM sys.foreign_keys fk
WHERE fk.parent_object_id = OBJECT_ID(N'dbo.caja_idempotencia')
ORDER BY fk.name;

SELECT
    cc.name AS check_name,
    cc.definition
FROM sys.check_constraints cc
WHERE cc.parent_object_id = OBJECT_ID(N'dbo.caja_idempotencia')
ORDER BY cc.name;

SELECT 'caja_idempotencia_rows' AS metric, COUNT_BIG(1) AS value
FROM dbo.caja_idempotencia;
