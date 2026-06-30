# Fase 4F.24 - Validacion visual controlada de lectura WPF mediante Caja API

Fecha/hora UTC: 2026-06-30 02:24:16 UTC

## Objetivo y alcance

Validar la preparacion visual de WPF para consultar estado de caja mediante `CajaApiClient`, sin escrituras, sin cambios de base de datos y sin fallback automatico a SQL cuando la API no este disponible.

La validacion cubre:

- estado de turno abierto para `CAJA_PRINCIPAL_TEST`;
- mensaje informativo cuando no existe turno abierto;
- manejo seguro de `401`, `403`, API detenida y ausencia de turno;
- confirmacion de integridad antes y despues.

No se abrieron turnos, no se registraron ingresos, no se registraron retiros, no se cerraron turnos y no se crearon idempotencias.

## Flags utilizados

Estado inicial en archivos versionados:

- `UseVentasApiWrite=false`;
- `EnableVentasApiWrite=false`;
- `EnableCajaApiWrite=false`;
- `UseCajaApiRead=false`;
- `UseCajaApiOpenWrite=false`;
- `UseCajaApiIngresoWrite=false`;
- `UseCajaApiRetiroWrite=false`;
- `UseCajaApiCierreWrite=false`.

Activacion temporal esperada para prueba visual manual:

- archivo: `PulperiaPOS/appsettings.Development.json` o `PulperiaPOS/appsettings.json`;
- valor temporal: `FeatureFlags:UseCajaApiRead=true`;
- no activar ningun flag de escritura;
- restaurar al finalizar: `FeatureFlags:UseCajaApiRead=false`.

Estado final confirmado en archivos versionados:

- todos los flags de lectura/escritura de Caja API quedaron en `false`;
- `EnableCajaApiWrite=false`;
- `EnableVentasApiWrite=false`.

## Pantallas WPF revisadas

- `IngresoCajaPage`;
- `RetirosCajaPage`;
- `CierreCajaPage`.

Se agrego un indicador visual no intrusivo y oculto por defecto. Cuando `UseCajaApiRead=true`, el indicador consulta `CajaApiClient` y muestra que el origen del estado es Caja API.

Con `UseCajaApiRead=false`, el indicador queda colapsado y no llama a la API.

## Endpoints consumidos

Durante la validacion automatizada no destructiva:

- `GET /health`;
- `GET /health/database`;
- `GET /api/system/version`;
- `GET /api/caja/turnos/abierto?cajaCodigo=CAJA_PRINCIPAL_TEST`.

En WPF, el helper visual queda preparado para usar:

- `GET /api/caja/turnos/abierto?cajaCodigo=CAJA_PRINCIPAL_TEST`;
- `GET /api/caja/turnos/{id}/movimientos`, solo si existe turno abierto;
- `GET /api/caja/turnos/{id}/pre-cierre`, solo si existe turno abierto.

## Permisos utilizados

La prueba autorizada requiere un usuario con permiso `Caja.Ver`.

No se documentan usuarios, passwords, tokens, rowVersion, llaves ni identificadores internos.

## Resultado de pruebas

### A. API disponible, usuario autorizado, sin turno abierto

Resultado:

- `/health`: HTTP 200;
- `/health/database`: HTTP 200;
- `/api/system/version`: HTTP 200;
- lectura autorizada de turno abierto: HTTP 204;
- WPF queda preparado para mostrar: `Caja API: No existe un turno de caja abierto para CAJA_PRINCIPAL_TEST.`;
- no se ejecutaron escrituras.

### B. API disponible, usuario sin permiso

Resultado:

- lectura sin permiso `Caja.Ver`: HTTP 403;
- `CajaApiErrorMapper` devuelve mensaje seguro de permiso insuficiente;
- no existe fallback SQL en el helper visual.

### C. Sesion invalida o expirada

Resultado:

- lectura sin token: HTTP 401;
- `ApiSessionCoordinator` y `CajaApiErrorMapper` usan mensaje seguro de sesion vencida;
- no existe fallback SQL en el helper visual.

### D. API detenida

Resultado:

- POS.Api detenido;
- puerto `7046` libre;
- llamada de lectura devuelve error de red controlado;
- `CajaApiErrorMapper` muestra mensaje seguro de servicio/red no disponible;
- no existe fallback SQL en el helper visual.

### E. Integridad antes y despues

Valores antes y despues:

- `caja_turno=3`;
- `movimiento_caja=9`;
- `caja_idempotencia` sin pendientes;
- turnos abiertos `0`;
- turnos `EnCierre=0`;
- turnos cerrados Test `3`;
- movimientos `CierreDiferencia=2`;
- `ingreso_caja=9`;
- `retiro_caja=6`;
- `cierre_caja=15`;
- `ventas=1948`;
- `venta_pago=10`;
- `venta_idempotencia=10`;
- inventario agregado `3296.00`;
- saldo agregado de clientes `-2957962.50`.

No hubo cambios de datos.

## Evidencia de ausencia de fallback SQL

El helper visual `CajaApiReadStatusViewHelper`:

- revisa `UseCajaApiRead`;
- si el flag esta apagado, colapsa el indicador y no llama a API;
- si el flag esta encendido, usa solamente `CajaApiClient`;
- no usa `DBConnection`;
- no ejecuta SQL;
- ante `401`, `403`, `404`, `409`, `503`, timeout o red caida, muestra mensaje seguro.

Las escrituras SQL historicas permanecen en los botones existentes de ingreso, retiro y cierre, pero no son usadas como fallback del estado leido por API.

## Cambios de codigo realizados

Archivos modificados:

- `PulperiaPOS/ApiClients/ApiClientBase.cs`;
- `PulperiaPOS/ApiClients/CajaApiErrorMapper.cs`;
- `PulperiaPOS/Models/Api/ApiErrorType.cs`;
- `PulperiaPOS/IngresoCajaPage.xaml`;
- `PulperiaPOS/IngresoCajaPage.xaml.cs`;
- `PulperiaPOS/Views/RetirosCajaPage.xaml`;
- `PulperiaPOS/Views/RetirosCajaPage.xaml.cs`;
- `PulperiaPOS/Views/CierreCajaPage.xaml`;
- `PulperiaPOS/Views/CierreCajaPage.xaml.cs`.

Archivo creado:

- `PulperiaPOS/CajaApiReadStatusViewHelper.cs`.

No se modificaron contratos API, base de datos, migraciones ni logica de idempotencia de escritura.

## Compilacion

- WPF: 0 errores. Persisten advertencias historicas preexistentes del proyecto en build directo.
- POS.Api: 0 errores, 0 advertencias.
- Solucion completa con salida aislada: 0 errores, 0 advertencias.

## Riesgos y advertencias

- La validacion visual manual requiere activar temporalmente `UseCajaApiRead=true` en configuracion local y autenticar un usuario con `Caja.Ver`.
- Las pantallas siguen cargando historiales SQL porque la ruta historica aun no se retira; esto no se usa como fallback del indicador API.
- No se probaron escrituras WPF por API en esta fase.

## Recomendacion Fase 4F.25

Ejecutar validacion manual guiada en WPF con `UseCajaApiRead=true`, API activa, usuario autorizado y usuario sin permiso, verificando visualmente el indicador en las tres pantallas antes de avanzar a apertura de turno por API.

