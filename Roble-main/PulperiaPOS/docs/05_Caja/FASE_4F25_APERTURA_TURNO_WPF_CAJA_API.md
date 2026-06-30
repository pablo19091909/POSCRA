# FASE 4F.25 - Apertura de turno WPF hacia Caja API

Fecha/hora UTC: 2026-06-30T03:31:02Z

## Objetivo y alcance

Se implemento la migracion controlada de una sola escritura de caja: apertura de turno mediante Caja API para `CAJA_PRINCIPAL_TEST`.

No se migraron ingresos, retiros, cierre, movimientos historicos, reportes, ventas, pagos, inventario, clientes ni saldos.

## Flujo historico auditado

| Punto auditado | Resultado |
| --- | --- |
| Pantalla WPF con caja | `IngresoCajaPage`, `RetirosCajaPage`, `CierreCajaPage`, `VentanaAdministrador`. |
| Boton historico `Abrir caja` | `VentanaAdministrador.AbrirCaja_Click`; abre gaveta fisica ESC/POS, no crea turno. |
| Flujo SQL legacy de apertura de turno | No se encontro flujo WPF legacy que inserte en `caja_turno`. |
| SQL legacy de ingresos | `IngresoCajaPage.RegistrarIngreso_Click` inserta en `ingreso_caja`; no fue modificado. |
| SQL legacy de retiros | `RetirosCajaPage.RegistrarRetiro_Click` inserta en `retiro_caja`; no fue modificado. |
| SQL legacy de cierres | `CierreCajaPage.GuardarCierre_Click` inserta en `cierre_caja`; no fue modificado. |
| Historiales visibles | Siguen usando SQL legacy. |

La nueva apertura API se agrego como accion explicita y aislada en `IngresoCajaPage`, visible solo cuando `UseCajaApiOpenWrite=true`.

## Endpoint y contrato

| Elemento | Valor |
| --- | --- |
| Endpoint | `POST /api/caja/turnos/abrir` |
| Autorizacion | JWT con permiso `Caja.Abrir` |
| Header | `Idempotency-Key` |
| Caja de prueba | `CAJA_PRINCIPAL_TEST` |
| Payload | `cajaCodigo`, `fondoInicial`, `observacion` |
| Respuesta exito | `CajaTurnoResponse` |

La API valida ambiente Test mediante `EnvironmentSafety` antes de permitir escrituras.

## Defecto detectado y correccion 010

Durante la prueba, apertura devolvio `503`. El diagnostico de metadatos mostro que `CK_caja_idempotencia_operacion` no permitia `AbrirTurno`; solo permitia `IngresoCaja`, `RetiroCaja`, `CerrarTurno`, `AjusteCaja` y `ReversaMovimiento`.

Se creo y aplico la migracion minima:

`database/migrations/010_CajaIdempotenciaAbrirTurno.sql`

Resultado:

`MIGRATION_010_CAJA_IDEMPOTENCIA_ABRIR_TURNO_OK`

La migracion solo ajusta el CHECK constraint para incluir `AbrirTurno`; no inserta, actualiza ni borra datos.

## Archivos modificados y creados

| Archivo | Cambio |
| --- | --- |
| `POS.Api/Controllers/CajaController.cs` | `AbrirTurno` ahora lee `Idempotency-Key`. |
| `POS.Api/Application/Caja/ICajaService.cs` | Firma de apertura recibe idempotency key. |
| `POS.Api/Application/Caja/CajaService.cs` | Valida key, calcula hash y llama repositorio idempotente. |
| `POS.Api/Application/Caja/ICajaRepository.cs` | Firma idempotente para apertura. |
| `POS.Api/Application/Caja/ICajaIdempotencyService.cs` | Hash canonico para apertura. |
| `POS.Api/Application/Caja/CajaIdempotencyService.cs` | Implementa hash de `AbrirTurno`. |
| `POS.Api/Application/Caja/CajaIdempotencyOperation.cs` | Agrega `AbrirTurno`. |
| `POS.Api/Infrastructure/Data/Caja/CajaRepository.cs` | Apertura transaccional idempotente con `caja_idempotencia`. |
| `PulperiaPOS/ApiClients/ApiClientBase.cs` | Soporte de headers por request. |
| `PulperiaPOS/ApiClients/CajaApiClient.cs` | Metodo `AbrirTurnoAsync`. |
| `PulperiaPOS/Models/Caja/AbrirCajaTurnoApiRequest.cs` | Contrato WPF para apertura. |
| `PulperiaPOS/IngresoCajaPage.xaml` | Panel aislado de apertura API. |
| `PulperiaPOS/IngresoCajaPage.xaml.cs` | Coordinador, confirmacion, idempotency key y mensajes seguros. |
| `database/migrations/010_CajaIdempotenciaAbrirTurno.sql` | Ajuste de constraint para `AbrirTurno`. |

## Feature flags

| Momento | `UseCajaApiRead` | `UseCajaApiOpenWrite` | Otras escrituras caja | Ventas API write |
| --- | --- | --- | --- | --- |
| Antes | `true` | `false` | `false` | `false` |
| Prueba API temporal | `true` | Habilitado por variable de entorno API `EnableCajaApiWrite=true` | `false` en WPF | `false` |
| Despues | `true` | `false` | `false` | `false` |

El archivo local WPF y su copia efectiva en `bin/Debug/net8.0-windows` quedaron con `UseCajaApiOpenWrite=false`.

## Evidencia de ruta unica API

Cuando `UseCajaApiOpenWrite=true`, el nuevo boton:

- usa `CajaOperationCoordinator`;
- genera/reutiliza una intencion por caja, fondo, observacion y usuario;
- envia `Idempotency-Key`;
- llama solo a `CajaApiClient.AbrirTurnoAsync`;
- no llama `DBConnection`;
- no ejecuta SQL directo;
- no llama el flujo legacy de ingreso/retiro/cierre.

Cuando `UseCajaApiOpenWrite=false`, el panel queda oculto y los flujos historicos existentes permanecen sin cambios.

## Idempotencia aplicada

| Capa | Comportamiento |
| --- | --- |
| WPF | `CajaOperationCoordinator` mantiene la misma key para la misma intencion. |
| API | Valida `Idempotency-Key` como GUID no vacio. |
| API | Calcula hash canonico de `AbrirTurno`. |
| SQL | Inserta `caja_idempotencia` en `EnProceso`, crea turno y movimiento `FondoInicial`, completa idempotencia. |
| Reintento misma key | Devuelve el resultado completado sin crear otro turno. |
| Nueva key con turno abierto | Devuelve conflicto. |

## Resultado de pruebas

| Prueba | Resultado |
| --- | --- |
| A. Flag apagado | `UseCajaApiOpenWrite=false`; panel WPF oculto; no hay llamada API desde WPF. |
| B. Apertura API exitosa | `open_before=204`, `open_first=200`, `turno_created=true`. |
| C. Doble clic / doble intento | Reintento con misma key: `open_retry_same_key=200`; no hubo duplicado. |
| D. Turno ya abierto | Nueva key despues de abrir: `open_second_new_key=409`. |
| E. Usuario sin permiso | `open_without_permission=403`. |
| F. API detenida/red caida | API temporal detenida; `api_temp_5077=000`. WPF no tiene fallback SQL en el boton API. |
| G. Timeout/estado incierto | No se simulo timeout destructivo; el codigo WPF mantiene la misma intencion en `Timeout`/`Network` y muestra estado incierto. |

La prueba real de apertura se ejecuto contra una instancia temporal de POS.Api en `http://127.0.0.1:5077` para evitar problemas de certificado en automatizacion. WPF conserva URL HTTPS configurada.

## Integridad antes y despues

| Indicador | Antes | Despues |
| --- | ---: | ---: |
| `turno_test_abierto` | 0 | 1 |
| `turno_test_encierre` | 0 | 0 |
| `caja_turno_total` | 3 | 4 |
| `movimiento_caja_total` | 9 | 10 |
| `mov_fondo_total_count` | 3 | 4 |
| `abrir_idemp_completada` | 0 | 1 |
| `idemp_enproceso_total` | 0 | 0 |
| `idemp_fallida_total` | 0 | 0 |
| `idemp_dup_user_operation_key` | 0 | 0 |
| `ingreso_caja_total` | 9 | 9 |
| `retiro_caja_total` | 6 | 6 |
| `cierre_caja_total` | 15 | 15 |
| `ventas_total` | 1948 | 1948 |
| `venta_pago_total` | 10 | 10 |
| `venta_idempotencia_total` | 10 | 10 |
| `inventario_stock_total` | 3296.00 | 3296.00 |
| `cliente_saldo_total` | -2957962.50 | -2957962.50 |

## Estado final del turno de prueba

Existe un turno abierto para `CAJA_PRINCIPAL_TEST`, creado por API, con un unico movimiento `FondoInicial` y una unica idempotencia `AbrirTurno` completada.

No se cerro el turno, no se registraron ingresos, no se registraron retiros y no se ejecuto limpieza manual.

## Compilacion

| Proyecto | Resultado |
| --- | --- |
| `POS.Api` | Compilacion correcta, 0 errores. |
| `PulperiaPOS` | Compilacion correcta, 0 errores. |
| Solucion completa | Compilacion correcta, 0 errores, 0 advertencias en validacion final. |

## Riesgos pendientes

- Queda un turno de prueba abierto en `CAJA_PRINCIPAL_TEST` como evidencia.
- Falta validacion visual manual del boton WPF con `UseCajaApiOpenWrite=true`.
- WPF aun conserva ingresos, retiros, cierre e historiales legacy.
- No se simulo timeout real a nivel de red despues de enviar request.
- Antes de produccion, `CAJA_PRINCIPAL_TEST` debe reemplazarse por configuracion de caja real controlada.

## Recomendacion para Fase 4F.26

Ejecutar validacion visual manual del boton de apertura WPF con `UseCajaApiOpenWrite=true`, confirmar que con turno ya abierto muestra conflicto seguro, y decidir el procedimiento aprobado para cerrar o conservar el turno de prueba antes de migrar ingresos de caja.
