# FASE 4F.24.2 - Diagnostico indicador Caja API WPF

Fecha/hora UTC: 2026-06-30T02:54:51Z

## Sintoma

El operador activo `UseCajaApiRead=true`, pero el indicador `Caja API` no aparecia en las pantallas WPF de caja.

Pantallas afectadas:

| Pantalla | Control esperado |
| --- | --- |
| IngresoCajaPage | `txtCajaApiStatus` |
| RetirosCajaPage | `txtCajaApiStatus` |
| CierreCajaPage | `txtCajaApiStatus` |

## Causa raiz encontrada

La aplicacion WPF estaba ejecutando artefactos de salida `Debug` desactualizados y la configuracion local efectiva de ese directorio no contenia la bandera nueva de lectura de Caja API.

Evidencia:

| Elemento | Resultado |
| --- | --- |
| Configuracion WPF versionada | `PulperiaPOS/appsettings.json` mantiene `UseCajaApiRead=false` por defecto. |
| Configuracion local WPF | `PulperiaPOS/appsettings.Development.json` no debe versionarse ni mostrarse; se completo localmente para la prueba. |
| Configuracion efectiva de ejecucion Debug | `PulperiaPOS/bin/Debug/net8.0-windows/appsettings.Development.json` ahora contiene `UseCajaApiRead=true` y escrituras apagadas. |
| Binario efectivo Debug | `PulperiaPOS/bin/Debug/net8.0-windows/PulperiaPOS.dll` actualizado tras compilacion normal. |
| Proyecto WPF | `appsettings.json` y `appsettings.Development.json` se copian con `CopyToOutputDirectory=PreserveNewest`; el archivo `.example` no se copia. |
| Carga de configuracion WPF | `AppConfiguration` lee desde `AppContext.BaseDirectory`, por lo que el runtime usa los JSON copiados a `bin/Debug/net8.0-windows`. |

Conclusion: activar el flag en un archivo local no era suficiente si el binario/configuracion de salida que realmente ejecutaba Visual Studio seguia viejo o si el archivo local efectivo no tenia la bandera.

## Configuracion y seguridad

| Archivo | Resultado |
| --- | --- |
| `.gitignore` | Cubre `appsettings.Development.json`, `POS.Api/appsettings.Development.json` y `**/appsettings.Development.json`. |
| `PulperiaPOS/appsettings.json` | Sin activacion de lectura Caja API por defecto; `UseCajaApiRead=false`. |
| `PulperiaPOS/appsettings.Development.json.example` | Sin secretos y con `UseCajaApiRead=false`. |
| `PulperiaPOS/appsettings.Development.json` | Archivo local ignorado; no se documentan ni muestran secretos. |
| `PulperiaPOS/bin/Debug/net8.0-windows/appsettings.Development.json` | Copia local de salida ignorada; usada para prueba manual con `UseCajaApiRead=true`. |

Flags efectivos validados en salida Debug:

| Flag | Valor |
| --- | --- |
| `UseCajaApiRead` | `true` |
| `UseVentasApiWrite` | `false` |
| `UseCajaApiOpenWrite` | `false` |
| `UseCajaApiIngresoWrite` | `false` |
| `UseCajaApiRetiroWrite` | `false` |
| `UseCajaApiCierreWrite` | `false` |

API base URL versionada para WPF: `https://localhost:7046/`.

Caja consultada por el indicador: `CAJA_PRINCIPAL_TEST`.

## Tabla de controles y eventos

| Pantalla | Control | Estado inicial | Evento de carga | Resultado esperado con flag false | Resultado esperado con flag true |
| --- | --- | --- | --- | --- | --- |
| `IngresoCajaPage` | `txtCajaApiStatus` | `Collapsed` | Constructor despues de `InitializeComponent` | Oculto y sin llamada API | Visible y consulta Caja API |
| `RetirosCajaPage` | `txtCajaApiStatus` | `Collapsed` | Constructor despues de `InitializeComponent` | Oculto y sin llamada API | Visible y consulta Caja API |
| `CierreCajaPage` | `txtCajaApiStatus` | `Collapsed` | Constructor despues de `InitializeComponent` | Oculto y sin llamada API | Visible y consulta Caja API |

## Tabla de origen de datos

| Dato en pantalla | Origen actual |
| --- | --- |
| Indicador `Caja API` | API, mediante `CajaApiClient` |
| Turno abierto para `CAJA_PRINCIPAL_TEST` | API `GET /api/caja/turnos/abierto` |
| Movimientos del turno abierto | API `GET /api/caja/turnos/{idTurno}/movimientos` solo si existe turno |
| Pre-cierre del turno abierto | API `GET /api/caja/turnos/{idTurno}/pre-cierre` solo si existe turno |
| Historiales y datos operativos existentes de las pantallas | SQL legado existente |
| Token de autenticacion | Solo memoria de `UserSession` |

No se agrego fallback SQL para el indicador Caja API.

## Log tecnico seguro

Se agrego log seguro en:

`%LOCALAPPDATA%/PulperiaPOS/Logs/caja-api-read.log`

Campos registrados:

| Campo | Contenido |
| --- | --- |
| Fecha/hora | UTC en formato ISO |
| Flag efectivo | `true` o `false` |
| Pantalla | Nombre tecnico de la pantalla |
| Caja consultada | Codigo de caja de prueba |
| API base URL | URL base no sensible |
| Token disponible | `true` o `false`, sin valor del token |
| Consulta iniciada | `true` o `false` |
| HTTP result | Codigo o categoria segura |
| Estado final del indicador | Visible u oculto |
| Mensaje UI asignado | Mensaje seguro mostrado al operador |

No se registran tokens, contrasenas, hashes, connection strings, usuarios ni detalles internos.

## Correccion aplicada

| Archivo | Cambio |
| --- | --- |
| `PulperiaPOS/CajaApiReadStatusViewHelper.cs` | Se agrego log seguro, manejo explicito de flag, estado visible inmediato al consultar y mensajes seguros para 204/401/403/API caida/error. |
| `PulperiaPOS/IngresoCajaPage.xaml.cs` | Se envia el nombre de pantalla al helper. |
| `PulperiaPOS/Views/RetirosCajaPage.xaml.cs` | Se envia el nombre de pantalla al helper. |
| `PulperiaPOS/Views/CierreCajaPage.xaml.cs` | Se envia el nombre de pantalla al helper. |
| `PulperiaPOS/appsettings.Development.json.example` | Se mantiene `UseCajaApiRead=false` por defecto. |
| `PulperiaPOS/appsettings.Development.json` | Se completo localmente para prueba, sin documentar valores sensibles. |

Comportamiento corregido:

| Condicion | Resultado |
| --- | --- |
| `UseCajaApiRead=false` | Indicador oculto, sin llamada API. |
| `UseCajaApiRead=true` | Indicador visible en las tres pantallas. |
| API responde 204 | Mensaje: `Caja API: No existe un turno de caja abierto para CAJA_PRINCIPAL_TEST.` |
| API responde 401/403 | Mensaje seguro de autenticacion/autorizacion. |
| API caida | Mensaje seguro de no disponibilidad. |

## Pruebas ejecutadas

| Prueba | Resultado |
| --- | --- |
| Compilacion WPF | Correcta, 0 errores, 0 advertencias. |
| Compilacion POS.Api | Correcta, 0 errores, 0 advertencias. |
| `/health` | HTTP 200. |
| `/health/database` | HTTP 200. |
| `/api/system/version` | HTTP 200. |
| Caja API sin token | HTTP 401. |
| Caja API con token sin permiso `caja.ver` | HTTP 403. |
| Caja API con token autorizado | HTTP 204, sin turno abierto para la caja de prueba. |
| Escrituras API | No ejecutadas. |
| Base de datos | Solo consultas de lectura y health checks. |

Prueba visual pendiente de confirmacion del operador:

| Escenario | Resultado esperado |
| --- | --- |
| Abrir `IngresoCajaPage` con API levantada y login API valido | Indicador visible con mensaje de no turno abierto o resumen de turno si existe. |
| Abrir `RetirosCajaPage` con API levantada y login API valido | Indicador visible con mensaje seguro. |
| Abrir `CierreCajaPage` con API levantada y login API valido | Indicador visible con mensaje seguro. |
| Apagar API y abrir pantallas | Indicador visible con mensaje seguro de API no disponible. |
| Volver a `UseCajaApiRead=false` | Indicador oculto, sin llamada API. |

## Integridad antes y despues

Se compararon snapshots agregados antes y despues. Los contadores criticos permanecieron iguales:

| Indicador | Antes | Despues |
| --- | ---: | ---: |
| `turno_test_abierto` | 0 | 0 |
| `turno_test_encierre` | 0 | 0 |
| `fase_movimientos_no_autorizados` | 0 | 0 |
| `fase_cierre_diferencia_negativa` | 0 | 0 |
| `fase_reversas` | 0 | 0 |
| `fase_movimientos_huerfanos` | 0 | 0 |
| `caja_turno_total` | 3 | 3 |
| `movimiento_caja_total` | 9 | 9 |
| `ingreso_caja_total` | 9 | 9 |
| `retiro_caja_total` | 6 | 6 |
| `cierre_caja_total` | 15 | 15 |
| `ventas_total` | 1948 | 1948 |
| `venta_pago_total` | 10 | 10 |
| `venta_idempotencia_total` | 10 | 10 |
| `inventario_stock_total` | 3296.00 | 3296.00 |
| `cliente_saldo_total` | -2957962.50 | -2957962.50 |

No se abrieron turnos, no se registraron ingresos, no se registraron retiros, no se cerraron turnos y no se modificaron ventas ni saldos.

## Riesgos pendientes

- La prueba visual final requiere que el operador abra las tres pantallas con POS.Api corriendo y sesion API valida.
- El indicador usa `CAJA_PRINCIPAL_TEST`; antes de produccion debe definirse como configuracion segura por terminal/caja real.
- Las pantallas de caja aun conservan lecturas SQL legadas para sus flujos historicos y operativos.
- Si Visual Studio ejecuta un binario viejo o no recompila, puede reaparecer el sintoma aunque el codigo fuente este correcto.
- El archivo local `appsettings.Development.json` debe mantenerse ignorado y no compartirse.

## Recomendacion

No cerrar definitivamente 4F.24.1 hasta que el operador confirme visualmente el indicador en `IngresoCajaPage`, `RetirosCajaPage` y `CierreCajaPage`.

Siguiente paso recomendado: ejecutar la prueba visual con API levantada, login API valido y `UseCajaApiRead=true`; despues volver el flag local a `false` si no se va a seguir probando lectura Caja API en esta estacion.
