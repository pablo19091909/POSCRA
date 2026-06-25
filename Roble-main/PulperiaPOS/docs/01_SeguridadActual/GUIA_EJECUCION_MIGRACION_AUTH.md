# Guia de ejecucion de migracion Auth

## Advertencia

Esta guia es para una fase posterior. En Fase 3B.1 los scripts fueron preparados, pero no ejecutados.

No copiar, imprimir ni documentar contrasenas, hashes, JWT signing keys, connection strings ni secretos.

## Orden recomendado

1. Hacer respaldo completo de Azure SQL.
2. Confirmar que WPF sigue operando antes de cambios.
3. Ejecutar primero `database/diagnostics/006_DiagnosticoUsuariosAuth.sql`.
4. Revisar usuarios duplicados, usuarios sin rol, usuarios sin nombre y columnas faltantes.
5. Corregir hallazgos manualmente solo con plan aprobado.
6. Ejecutar manualmente `database/migrations/006_AuthModernizacionUsuarios.sql`.
7. Ejecutar nuevamente diagnostico.
8. Validar que las columnas fueron creadas.
9. Configurar JWT local o variable `POS_API_JWT_SIGNING_KEY`.
10. Mantener `Authentication:EnableLegacyHashUpgrade=false`.
11. Probar `POST /api/auth/login` con un usuario de prueba.
12. Activar `EnableLegacyHashUpgrade=true` solo despues de validar migracion y respaldo.
13. Monitorear migraciones de hash sin registrar valores sensibles.

## Configuracion local

Usar `POS.Api/appsettings.Development.json` local ignorado o variables de entorno.

Valores requeridos:

- `POS_API_DATABASE_CONNECTION_STRING`
- `POS_API_JWT_SIGNING_KEY`

Opcional:

- `Authentication:EnableLegacyHashUpgrade`
- `RateLimiting:Login:PermitLimit`
- `RateLimiting:Login:WindowSeconds`

## Validacion posterior a migracion

Ejecutar:

- `GET /health`
- `GET /health/database`
- `GET /api/system/version`
- `POST /api/auth/login` con usuario de prueba

Confirmar:

- login exitoso emite JWT;
- usuario inactivo no entra;
- usuario bloqueado no entra;
- hash legado valido puede entrar;
- si `EnableLegacyHashUpgrade=false`, no se escribe `password_hash_v2`;
- si `EnableLegacyHashUpgrade=true`, solo usuario de prueba migra a BCrypt.

## Desactivar migracion automatica

Si hay comportamiento inesperado:

1. Cambiar `Authentication:EnableLegacyHashUpgrade=false`.
2. Reiniciar `POS.Api`.
3. Verificar que el login sigue validando sin actualizar hashes.
4. Investigar con logs seguros y `traceId`.

## Rollback

Usar `database/rollback/006_AuthModernizacionUsuarios_rollback.sql` solo si:

- no hay usuarios usando `password_hash_v2`;
- el diagnostico confirma que no hay hashes modernos;
- existe respaldo reciente;
- se entiende que el rollback elimina columnas agregadas por la migracion.

El rollback se detiene si detecta hashes modernos.

## Verificar que secretos no se suban

Antes de commit:

```powershell
git status --ignored -- POS.Api/appsettings.Development.json
git check-ignore -v POS.Api/appsettings.Development.json
```

Tambien revisar que:

- `POS.Api/appsettings.json` no tenga secretos reales;
- `POS.Api/appsettings.Development.json.example` solo tenga placeholders;
- documentos no incluyan valores reales.
