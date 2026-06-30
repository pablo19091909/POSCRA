# Fase 4F.24.1 - Prueba manual guiada y mapa de fuentes de lectura Caja WPF

Fecha/hora UTC: 2026-06-30 02:30:52 UTC

Fecha/hora local aproximada: 2026-06-29 20:30:53

## Objetivo

Completar la delimitacion tecnica de la validacion visual de lectura de Caja API en WPF y documentar con precision que datos provienen de API, SQL historico, sesion o memoria.

Esta fase es solo lectura. No se migraron apertura, ingreso, retiro, cierre ni historiales completos de Caja.

## Flags antes, durante y despues

Estado antes de la validacion:

- `UseCajaApiRead=false`;
- `UseVentasApiWrite=false`;
- `UseCajaApiOpenWrite=false`;
- `UseCajaApiIngresoWrite=false`;
- `UseCajaApiRetiroWrite=false`;
- `UseCajaApiCierreWrite=false`;
- `EnableVentasApiWrite=false`;
- `EnableCajaApiWrite=false`.

Estado durante prueba manual requerida:

- archivo local recomendado: `PulperiaPOS/appsettings.Development.json`;
- activar temporalmente: `FeatureFlags:UseCajaApiRead=true`;
- no activar ningun flag de escritura.

Estado final confirmado en archivos versionados:

- `UseCajaApiRead=false`;
- todos los flags de escritura siguen en `false`;
- `EnableCajaApiWrite=false`;
- `EnableVentasApiWrite=false`.

Nota: no se imprimio ni documento contenido de configuracion local con secretos.

## Pruebas A, B, C y D

### A. Usuario autorizado, API disponible, sin turno abierto

Resultado tecnico no destructivo:

- `/health=200`;
- `/health/database=200`;
- `/api/system/version=200`;
- `GET /api/caja/turnos/abierto` con permiso `Caja.Ver`: HTTP 204;
- no hay turno abierto para `CAJA_PRINCIPAL_TEST`.

Resultado visual esperado en las tres pantallas:

- `Caja API: No existe un turno de caja abierto para CAJA_PRINCIPAL_TEST.`

Estado: listo para confirmacion manual del operador en WPF con `UseCajaApiRead=true`.

### B. Usuario sin permiso

Resultado tecnico no destructivo:

- `GET /api/caja/turnos/abierto` sin permiso `Caja.Ver`: HTTP 403.

Resultado visual esperado:

- mensaje seguro de permiso insuficiente desde `CajaApiErrorMapper`;
- la pantalla no debe bloquearse;
- el indicador API no debe consultar SQL como sustituto.

Estado: listo para confirmacion manual del operador.

### C. Sesion invalida o token no disponible

Resultado tecnico no destructivo:

- `GET /api/caja/turnos/abierto` sin token: HTTP 401.

Resultado visual esperado:

- mensaje seguro de sesion vencida o necesidad de iniciar sesion nuevamente;
- no mostrar informacion vieja como actual;
- no usar fallback SQL para el indicador API.

Estado: listo para confirmacion manual del operador.

### D. API detenida

Resultado tecnico no destructivo:

- POS.Api detenido;
- puerto `7046` libre;
- llamada de lectura: `HttpRequestException`.

Resultado visual esperado:

- mensaje seguro de problema de conexion o servicio no disponible;
- WPF no debe bloquearse;
- el indicador API no debe usar SQL como respaldo.

Estado: listo para confirmacion manual del operador.

## Resultado visual por pantalla

La validacion visual manual debe confirmar:

- `IngresoCajaPage`: indicador API visible con `UseCajaApiRead=true` y mensaje sin turno abierto, permiso insuficiente, sesion vencida o red caida segun escenario.
- `RetirosCajaPage`: mismo comportamiento del indicador API.
- `CierreCajaPage`: mismo comportamiento del indicador API.

Sin `UseCajaApiRead=true`, el indicador queda oculto y no llama a Caja API.

## Tabla de fuentes de datos

| Pantalla | Elemento visual o dato | Fuente actual | Tipo | Metodo o archivo que lo carga | Es legado | Es fallback | Recomendacion futura |
| -------- | ---------------------- | ------------- | ---- | ----------------------------- | --------- | ----------- | -------------------- |
| IngresoCajaPage | Indicador visual nuevo de estado API | Caja API | API | `CajaApiReadStatusViewHelper.LoadAsync` usando `CajaApiClient.GetTurnoAbiertoAsync` | No | No | Mantener como primera lectura API visible |
| IngresoCajaPage | Estado de turno abierto | Caja API cuando `UseCajaApiRead=true` | API | `CajaApiReadStatusViewHelper.LoadAsync` | No | No | Extender a selector de caja si se generaliza |
| IngresoCajaPage | Historial de ingresos | `ingreso_caja` | SQL | `IngresoCajaPage.CargarIngresos` con `DBConnection` | Si | No | Migrar despues de endpoint de movimientos/reporte |
| IngresoCajaPage | Total en Caja Actual | ventas, ingresos, retiros historicos | SQL | `CajaHelper.ObtenerDineroAcumuladoCajaChica` | Si | No | Reemplazar por resumen de turno API cuando caja API sea fuente principal |
| IngresoCajaPage | Usuario de ingreso historico | `UserSession.NombreUsuario` | Sesion | `RegistrarIngreso_Click` | Si | No | Usar claim/token API en escritura futura |
| RetirosCajaPage | Indicador visual nuevo de estado API | Caja API | API | `CajaApiReadStatusViewHelper.LoadAsync` | No | No | Mantener para lectura de turno |
| RetirosCajaPage | Estado de turno abierto | Caja API cuando `UseCajaApiRead=true` | API | `CajaApiReadStatusViewHelper.LoadAsync` | No | No | Reusar para validar retiro con turno abierto |
| RetirosCajaPage | Dinero en Caja calculado | ventas, ingresos, retiros historicos | SQL | `CajaHelper.ObtenerDineroAcumuladoCajaChica` | Si | No | Reemplazar por efectivo esperado API |
| RetirosCajaPage | Validacion de monto disponible | memoria derivada de SQL historico | Memoria/SQL | campo `dineroDisponibleEnCaja` cargado por `CalcularDineroEnCaja` | Si | No | Mover validacion al endpoint de retiro API |
| RetirosCajaPage | Historial de retiros | `retiro_caja` | SQL | `RetirosCajaPage.CargarRetiros` con `DBConnection` | Si | No | Migrar a movimientos de turno API |
| RetirosCajaPage | Usuario impreso en recibo | `UserSession.NombreUsuario` | Sesion | `RegistrarRetiro_Click` | Si | No | Usar usuario autenticado API en escritura futura |
| CierreCajaPage | Indicador visual nuevo de estado API | Caja API | API | `CajaApiReadStatusViewHelper.LoadAsync` | No | No | Usar para pre-cierre visual antes de cerrar por API |
| CierreCajaPage | Estado de turno abierto | Caja API cuando `UseCajaApiRead=true` | API | `CajaApiReadStatusViewHelper.LoadAsync` | No | No | Mostrar pre-cierre completo en fase posterior |
| CierreCajaPage | Datos de pre-cierre API | Caja API solo si existe turno abierto | API | `CajaApiClient.GetPreCierreAsync` desde helper | No | No | Convertir en fuente principal de cierre API |
| CierreCajaPage | Total en efectivo | ventas, ingresos, retiros historicos | SQL | `CajaHelper.ObtenerTotalesCaja` | Si | No | Sustituir por efectivo esperado API |
| CierreCajaPage | SINPE/Datafono visibles | ventas y saldos historicos | SQL | `CalcularTotalesDelDia` con `DBConnection` y `CajaHelper` | Si | No | Separar de cierre de turno API o crear reporte especifico |
| CierreCajaPage | Historial de cierres anteriores | `cierre_caja` | SQL | `CargarCierresAnteriores` con `DBConnection` | Si | No | Migrar a reporte por turno API |
| CierreCajaPage | Usuario impreso en cierre historico | `UserSession.NombreUsuario` | Sesion | `GuardarCierre_Click` | Si | No | Usar usuario autenticado API en cierre futuro |

## Criterio de transparencia

El indicador API no usa fallback SQL.

Las pantallas aun mantienen lecturas historicas SQL para historiales, totales y validaciones de caja.

Esto no representa fallback del indicador API, pero si representa dependencia SQL legacy de la pantalla.

La migracion total de lectura no esta completa.

## Evidencia de ausencia de fallback SQL

Revision estatica:

- `CajaApiReadStatusViewHelper` usa `CajaApiClient`;
- `CajaApiReadStatusViewHelper` no usa `DBConnection`;
- `CajaApiReadStatusViewHelper` no ejecuta SQL;
- si `UseCajaApiRead=false`, el indicador se oculta y no llama a API;
- errores `401`, `403`, red caida y ausencia de turno se manejan como mensajes seguros.

Las consultas SQL existentes en las paginas pertenecen a lecturas o escrituras historicas de pantalla, no al indicador API.

## Integridad antes y despues

Valores antes:

- `caja_turno=3`;
- `movimiento_caja=9`;
- `CierreDiferencia=2`;
- turnos abiertos `0`;
- turnos `EnCierre=0`;
- turnos cerrados Test `3`;
- idempotencias `CerrarTurno Completada=3`;
- idempotencias pendientes `0`;
- `ingreso_caja=9`;
- `retiro_caja=6`;
- `cierre_caja=15`;
- `ventas=1948`;
- `venta_pago=10`;
- `venta_idempotencia=10`;
- inventario agregado `3296.00`;
- saldo agregado de clientes `-2957962.50`.

Valores despues:

- `caja_turno=3`;
- `movimiento_caja=9`;
- `CierreDiferencia=2`;
- turnos abiertos `0`;
- turnos `EnCierre=0`;
- turnos cerrados Test `3`;
- idempotencias `CerrarTurno Completada=3`;
- idempotencias pendientes `0`;
- `ingreso_caja=9`;
- `retiro_caja=6`;
- `cierre_caja=15`;
- `ventas=1948`;
- `venta_pago=10`;
- `venta_idempotencia=10`;
- inventario agregado `3296.00`;
- saldo agregado de clientes `-2957962.50`.

Resultado: sin cambios de datos.

## Cambios realizados

No se realizaron cambios de codigo en esta fase.

Se creo este documento de delimitacion y prueba guiada.

## Compilacion

- WPF: compilacion correcta, 0 advertencias y 0 errores con salida aislada.
- POS.Api: compilacion correcta, 0 advertencias y 0 errores.
- Solucion completa: compilacion correcta, 0 advertencias y 0 errores con salida aislada.

## Riesgos y limitaciones vigentes

- La confirmacion visual final requiere ejecucion manual del operador en WPF.
- Las pantallas siguen cargando historiales y totales desde SQL legacy.
- No se migraron movimientos historicos a API.
- No se conecto `CajaOperationCoordinator` a botones.
- No se probaron escrituras WPF por API.

## Recomendacion para la siguiente fase

Avanzar a Fase 4F.25 solo despues de que el operador confirme visualmente los escenarios A, B, C y D en las tres pantallas.

La siguiente migracion debe ser una sola escritura controlada: apertura de turno por API, manteniendo ingreso, retiro y cierre aun apagados.

