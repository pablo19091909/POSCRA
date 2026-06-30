# FASE 4F.26 - Validacion visual apertura WPF Caja API

Fecha/hora UTC: 2026-06-30T03:59:18Z

## Alcance

Validacion controlada de apertura de turno WPF contra Caja API usando el turno Test ya abierto en `CAJA_PRINCIPAL_TEST`.

No se autorizo crear un nuevo turno. No se cerraron turnos, no se registraron ingresos, retiros, cierres, ajustes, reversas ni movimientos.

## Linea base

| Indicador | Valor |
| --- | ---: |
| `environment_test_allowed` | 1 |
| `turno_test_abierto` | 1 |
| `turno_test_encierre` | 0 |
| `caja_turno_total` | 4 |
| `movimiento_caja_total` | 10 |
| `mov_fondo_total_count` | 4 |
| `idemp_enproceso_total` | 0 |
| `idemp_fallida_total` | 0 |
| `idemp_dup_user_operation_key` | 0 |
| `ingreso_caja_total` | 9 |
| `retiro_caja_total` | 6 |
| `cierre_caja_total` | 15 |
| `ventas_total` | 1948 |
| `venta_pago_total` | 10 |
| `venta_idempotencia_total` | 10 |
| `inventario_stock_total` | 3296.00 |
| `cliente_saldo_total` | -2957962.50 |

## Flags

| Momento | Resultado |
| --- | --- |
| Inicial versionado | Escrituras apagadas. |
| Local detectado al inicio | Se encontro `UseCajaApiOpenWrite=true` en archivo local ignorado; se restauro a `false` antes de iniciar la validacion. |
| Activacion temporal | `UseCajaApiOpenWrite=true` solo en configuracion local ignorada para compilar WPF. |
| API temporal | `EnableCajaApiWrite=true`, `EnableVentasApiWrite=false`, ambiente requerido `Test`. |
| Final | `UseCajaApiRead=true`; `UseCajaApiOpenWrite=false`; ingreso/retiro/cierre/ventas write `false`. |

## Validacion API y conflicto

| Prueba | Resultado |
| --- | --- |
| `/health` | 200 |
| `/health/database` | 200 |
| `/api/system/version` | 200 |
| Apertura con turno ya abierto | 409 |
| Usuario sin permiso de apertura | 403 |
| API detenida | 000, sin respuesta |

La prueba de conflicto no creo registros:

| Conteo | Antes | Despues |
| --- | ---: | ---: |
| `caja_turno` | 4 | 4 |
| `movimiento_caja` | 10 | 10 |
| `caja_idempotencia` | 8 | 8 |
| turno abierto `CAJA_PRINCIPAL_TEST` | 1 | 1 |

## Validacion visual WPF

Se preparo WPF con el flag temporal y se compilo para que `bin/Debug/net8.0-windows` tuviera `UseCajaApiOpenWrite=true` durante la ventana de validacion.

Limitacion: la interaccion visual real dentro de WPF requiere operador humano. En esta ejecucion no se confirmo visualmente una captura de pantalla ni clic manual del operador. No se inventa esa evidencia.

Validaciones por codigo aplicables a la UX:

- el panel `Apertura de Turno API` solo aparece con `UseCajaApiOpenWrite=true`;
- muestra `Caja API activa para apertura`;
- solicita confirmacion antes de enviar;
- deshabilita el boton durante el envio;
- ante `409`, muestra `Ya existe un turno abierto para esta caja.`;
- ante red/API caida, muestra `No fue posible comunicarse con el servicio de caja. Intente nuevamente.`;
- no muestra SQL, stack trace, token, idempotency key ni endpoint;
- no hay apertura automatica al entrar a la pantalla.

## Integridad posterior

La linea base posterior coincide con la inicial para esta fase:

| Indicador | Valor posterior |
| --- | ---: |
| `turno_test_abierto` | 1 |
| `turno_test_encierre` | 0 |
| `caja_turno_total` | 4 |
| `movimiento_caja_total` | 10 |
| `idemp_enproceso_total` | 0 |
| `idemp_fallida_total` | 0 |
| `ingreso_caja_total` | 9 |
| `retiro_caja_total` | 6 |
| `cierre_caja_total` | 15 |
| `ventas_total` | 1948 |
| `inventario_stock_total` | 3296.00 |
| `cliente_saldo_total` | -2957962.50 |

## Compilacion

| Proyecto | Resultado |
| --- | --- |
| WPF | Correcto |
| API | Correcto |
| Solucion completa | Correcta, 0 errores, 0 advertencias en validacion final |

## Estado final

- POS.Api temporal detenida.
- Puerto `7046` sin escucha activa.
- Puerto temporal `5077` sin respuesta.
- `UseCajaApiOpenWrite=false`.
- No se modifico el turno abierto existente.

## Recomendacion

Fase 4F.27: ejecutar validacion manual asistida por operador dentro de WPF con checklist en mano, o avanzar al diseno de integracion de ingreso de caja API solo despues de confirmar visualmente el comportamiento del conflicto.
