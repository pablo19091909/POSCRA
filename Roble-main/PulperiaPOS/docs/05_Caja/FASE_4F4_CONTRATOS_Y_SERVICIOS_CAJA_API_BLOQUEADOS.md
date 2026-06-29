# Fase 4F.4 - Contratos y servicios Caja API bloqueados

Fecha/hora UTC: 2026-06-28 17:20:07 UTC.

## Resultado

Se creo la estructura inicial de Caja API en POS.Api con contratos, permisos, controlador, servicio y repositorio de lectura. Toda escritura queda bloqueada por `FeatureFlags:EnableCajaApiWrite=false`.

## Modelo real usado

- `caja_turno`: `idTurno`, `caja_codigo`, `estado`, usuarios de apertura/cierre, UTC de apertura/cierre, fondo inicial, esperado, contado, diferencia, observaciones, `cierre_caja_id`, `row_version`.
- `movimiento_caja`: `idMovimiento`, `idTurno`, tipo, origen, monto, moneda, UTC, usuario, referencias nullable a factura, pago, ingreso, retiro y reversa.
- Indice unico: `UX_caja_turno_un_abierto_por_caja`.
- FKs hacia `usuario`, `ventas`, `venta_pago`, `ingreso_caja`, `retiro_caja`, `cierre_caja` y reversa.

## Componentes creados

- `POS.Api/Controllers/CajaController.cs`
- `POS.Api/Contracts/Caja/*`
- `POS.Api/Application/Caja/*`
- `POS.Api/Infrastructure/Data/Caja/CajaRepository.cs`

## Componentes modificados

- `POS.Api/Configuration/FeatureFlagsOptions.cs`
- `POS.Api/Domain/PermissionNames.cs`
- `POS.Api/Infrastructure/Security/RolePermissionProvider.cs`
- `POS.Api/Application/Ventas/IDatabaseEnvironmentSafetyService.cs`
- `POS.Api/Infrastructure/Data/Ventas/DatabaseEnvironmentSafetyService.cs`
- `POS.Api/Program.cs`
- `POS.Api/appsettings.json`
- `POS.Api/appsettings.Development.json.example`
- `POS.Api/appsettings.Test.json`

## Endpoints preparados

- `GET /api/caja/turnos/abierto?cajaCodigo=...`
- `POST /api/caja/turnos/abrir`
- `POST /api/caja/ingresos`
- `POST /api/caja/retiros`
- `GET /api/caja/turnos/{id}/pre-cierre`
- `POST /api/caja/turnos/{id}/cerrar`
- `GET /api/caja/turnos/{id}/movimientos`

## Pruebas no destructivas

| Prueba | Resultado |
| --- | --- |
| `/health` | 200 |
| `/health/database` | 200 |
| `/api/system/version` | 200 |
| Caja sin token | 401 |
| Caja con token sin permiso | 403 |
| Turno abierto con `Caja.Ver` y sin turnos | 204 seguro |
| Pre-cierre de turno inexistente | 404 |
| Abrir turno con permiso y flag apagado | 503 |

La prueba de tokens uso una signing key temporal no secreta solo en el proceso de API, sin modificar archivos.

## Compilacion

- WPF: `PulperiaPOS/PulperiaPOS.csproj` compilo correctamente, 0 advertencias, 0 errores.
- POS.Api: `POS.Api/POS.Api.csproj` compilo correctamente, 0 advertencias, 0 errores.
- Solucion completa: compilo correctamente durante la validacion de la fase, 0 advertencias, 0 errores.

## Integridad

Caja API no creo turnos ni movimientos:

- `caja_turno=0`
- `movimiento_caja=0`
- `ingreso_caja=9`
- `retiro_caja=6`
- `cierre_caja=15`

Durante la fase se detectaron cambios concurrentes externos en ventas, stock y saldo respecto a la linea base tomada al inicio. No provinieron de Caja API: los endpoints de escritura de caja respondieron 503 y las tablas nuevas permanecieron vacias.

## Recomendacion

Avanzar a Fase 4F.5 para implementar apertura de turno en POS.Api, todavia controlada por `EnableCajaApiWrite=false` por defecto, con una prueba posterior en Test solo cuando se autorice activar el flag localmente.
