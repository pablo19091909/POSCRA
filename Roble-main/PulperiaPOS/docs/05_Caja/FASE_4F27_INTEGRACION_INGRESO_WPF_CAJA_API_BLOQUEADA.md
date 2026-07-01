# FASE 4F.27 - Integracion ingreso WPF Caja API bloqueada

Fecha/hora UTC: 2026-06-30T13:45:44Z

## Objetivo

Preparar `IngresoCajaPage` para registrar ingresos mediante `POST /api/caja/ingresos`, manteniendo la escritura desactivada con `UseCajaApiIngresoWrite=false`.

No se crearon ingresos, movimientos, idempotencias, retiros, cierres, turnos, ventas ni pagos.

## Auditoria historica

| Elemento | Resultado |
| --- | --- |
| Pantalla | `IngresoCajaPage` |
| Boton historico | `Registrar Ingreso` |
| Metodo historico | `RegistrarIngresoHistoricoSql` |
| Tabla historica | `ingreso_caja` |
| SQL historico | Inserta `monto`, `motivo`, `fecha`, `hora`, `usuario` |
| Usuario historico | `UserSession.NombreUsuario` |
| Fecha/hora historica | Generada en WPF |
| Gaveta fisica | `RawPrinterHelper.AbrirCajaDesdePOS58()` despues del INSERT historico |
| Totales historicos | `CajaHelper.ObtenerDineroAcumuladoCajaChica()` |
| Historial | `SELECT * FROM ingreso_caja ORDER BY fecha DESC, hora DESC` |

El flujo historico permanece detras de `UseCajaApiIngresoWrite=false` y no se cambio su SQL.

## Integracion preparada

| Archivo | Cambio |
| --- | --- |
| `PulperiaPOS/ApiClients/CajaApiClient.cs` | Agrega `RegistrarIngresoAsync`. |
| `PulperiaPOS/Models/Caja/CajaIngresoRequest.cs` | Modelo de request WPF. |
| `PulperiaPOS/Models/Caja/CajaIngresoResult.cs` | Resultado de ingreso WPF. |
| `PulperiaPOS/Models/Caja/CajaIngresoViewModel.cs` | View model local de validacion. |
| `PulperiaPOS/IngresoCajaPage.xaml` | Etiqueta de modo, estado API y referencia opcional. |
| `PulperiaPOS/IngresoCajaPage.xaml.cs` | Bifurcacion por feature flag, validaciones, idempotencia y mensajes seguros. |

## Feature flag

| Flag | Estado final |
| --- | --- |
| `UseCajaApiRead` | `true` |
| `UseCajaApiOpenWrite` | `false` |
| `UseCajaApiIngresoWrite` | `false` |
| `UseCajaApiRetiroWrite` | `false` |
| `UseCajaApiCierreWrite` | `false` |
| `UseVentasApiWrite` | `false` |
| `EnableCajaApiWrite` | `false`, sin proceso API activo |
| `EnableVentasApiWrite` | `false`, sin proceso API activo |

## Politica sin fallback

Cuando `UseCajaApiIngresoWrite=true`, la accion:

- usa `CajaApiClient.GetTurnoAbiertoAsync`;
- usa `CajaApiClient.RegistrarIngresoAsync`;
- envia `Idempotency-Key` por header;
- no llama `DBConnection`;
- no inserta en `ingreso_caja`;
- no llama `CajaHelper` para persistir ingresos;
- no hace fallback a SQL ante errores.

Cuando `UseCajaApiIngresoWrite=false`, se ejecuta el flujo historico SQL existente.

## Pruebas no destructivas

| Prueba | Resultado |
| --- | --- |
| Sin token | 401 |
| Token sin permiso `Caja.Ingresar` | 403 |
| Token con permiso, escritura apagada y sin key | 503 |
| Token con permiso, escritura apagada y key invalida | 503 |
| Token con permiso, escritura apagada y key valida | 503 |
| Health | 200 |
| Health database | 200 |
| Version | 200 |

## Integridad

| Conteo | Antes | Despues |
| --- | ---: | ---: |
| `caja_turno` | 4 | 4 |
| `movimiento_caja` | 10 | 10 |
| `caja_idempotencia` | 8 | 8 |
| `ingreso_caja` | 9 | 9 |
| `retiro_caja` | 6 | 6 |
| `cierre_caja` | 15 | 15 |
| `ventas` | 1948 | 1948 |
| `venta_pago` | 10 | 10 |
| `venta_idempotencia` | 10 | 10 |
| `inventario_stock_total` | 3296.00 | 3296.00 |
| `cliente_saldo_total` | -2957962.50 | -2957962.50 |

## Compilacion

| Proyecto | Resultado |
| --- | --- |
| WPF | Correcto, 0 errores. |
| API | Correcto, 0 errores. |
| Solucion completa | Correcta, 0 errores, 0 advertencias en validacion final. |

## Limitaciones

- `UseCajaApiIngresoWrite` no fue activado.
- No se registro un ingreso sintetico.
- Retiro WPF sigue apagado.
- Cierre WPF sigue apagado.
- Sin reversas.
- Sin ventas efectivas API conectadas a caja.
- Sin dolares, donaciones ni pagos combinados integrados a caja API.

## Recomendacion

Fase 4F.28: habilitacion temporal y prueba manual controlada de un unico ingreso sintetico por Caja API, con `UseCajaApiIngresoWrite=true` y `EnableCajaApiWrite=true` solo durante la prueba.
