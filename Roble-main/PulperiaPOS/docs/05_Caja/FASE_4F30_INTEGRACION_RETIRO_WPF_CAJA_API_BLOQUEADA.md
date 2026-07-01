# Fase 4F.30 - Integracion retiro WPF Caja API bloqueada

Fecha UTC: 2026-06-30.

## Resultado

Se preparo `RetirosCajaPage` para registrar retiros mediante Caja API detras de `UseCajaApiRetiroWrite=false`. La ruta queda compilada, pero bloqueada. No se activo el flag y no se creo retiro, movimiento, idempotencia, turno, cierre, ajuste, reversa, venta ni cambio historico.

## Auditoria de flujo historico

`RetirosCajaPage` historica:

- usa `double` para monto y efectivo disponible;
- calcula efectivo disponible con `CajaHelper.ObtenerDineroAcumuladoCajaChica()`;
- compara monto localmente contra ese efectivo;
- usa fecha y hora local;
- inserta en `retiro_caja` mediante SQL directo;
- abre gaveta e imprime comprobante con `RawPrinterHelper`;
- carga historial desde `retiro_caja`;
- no tenia bloqueo explicito de doble clic;
- no usa transaccion de caja operacional.

La ruta historica no fue reemplazada. Con `UseCajaApiRetiroWrite=false`, sigue siendo la ruta activa.

## Integracion bloqueada

Archivos creados:

- `PulperiaPOS/Models/Caja/CajaRetiroRequest.cs`
- `PulperiaPOS/Models/Caja/CajaRetiroResult.cs`
- `PulperiaPOS/Models/Caja/CajaRetiroViewModel.cs`

Archivos modificados:

- `PulperiaPOS/ApiClients/CajaApiClient.cs`
- `PulperiaPOS/Views/RetirosCajaPage.xaml`
- `PulperiaPOS/Views/RetirosCajaPage.xaml.cs`

La nueva ruta WPF API:

- usa `decimal`;
- valida monto positivo;
- exige motivo;
- acepta referencia opcional;
- no envia usuario, turno, fecha, estado, efectivo disponible, hash ni movimiento;
- envia `Idempotency-Key` solo por header;
- usa `CajaOperationCoordinator`;
- consulta turno y pre-cierre por API;
- no usa SQL directo, `CajaHelper` ni `RawPrinterHelper`;
- no tiene fallback SQL.

## Feature flag

| Flag | Resultado |
| --- | --- |
| `UseCajaApiRetiroWrite=false` | Flujo historico activo, sin llamada API de retiro |
| `UseCajaApiRetiroWrite=true` | Ruta preparada para usar solo Caja API |

El flag no fue activado en esta fase.

## Pruebas no destructivas

Con POS.Api temporal y `EnableCajaApiWrite=false`:

| Escenario | Resultado |
| --- | --- |
| POST sin token | 401 |
| POST con token sin `Caja.Retirar` | 403 |
| POST con permiso y sin key | 503 |
| POST con permiso y key invalida | 503 |
| POST con permiso y key valida | 503 |

Health checks:

| Endpoint | Resultado |
| --- | --- |
| `/health` | 200 |
| `/health/database` | 200 |
| `/api/system/version` | 200 |

## Integridad

| Metrica | Antes | Despues |
| --- | ---: | ---: |
| `caja_turno` | 4 | 4 |
| Turnos `Abierto` Test | 1 | 1 |
| Turnos `EnCierre` Test | 0 | 0 |
| `movimiento_caja` | 11 | 11 |
| `caja_idempotencia` | 9 | 9 |
| Idempotencias `EnProceso` | 0 | 0 |
| `ingreso_caja` | 9 | 9 |
| `retiro_caja` | 6 | 6 |
| `cierre_caja` | 15 | 15 |
| `ventas` | 1948 | 1948 |
| `venta_pago` | 10 | 10 |
| `venta_idempotencia` | 10 | 10 |
| Inventario agregado | 3296.00 | 3296.00 |
| Saldo cliente agregado | -2957962.50 | -2957962.50 |
| Efectivo esperado | 1100.00 | 1100.00 |

## Compilacion

| Proyecto | Resultado |
| --- | --- |
| Solucion completa | Correcta, 0 advertencias, 0 errores |
| WPF salida normal | Correcta, 0 advertencias, 0 errores |
| POS.Api | Correcta, 0 advertencias, 0 errores |

## Restauracion

POS.Api temporal fue detenida y el puerto local configurado quedo sin escucha activa. Todos los flags de escritura quedaron apagados.

## Limitaciones pendientes

- Cierre WPF todavia apagado.
- Sin reversas.
- Sin ventas efectivas API integradas a Caja API.
- Sin Dolares, Donacion ni pagos combinados.

## Recomendacion

Continuar con Fase 4F.31: validacion visual manual de `RetirosCajaPage` con `UseCajaApiRetiroWrite=false`, confirmando que el modo historico sigue intacto y que la UX API permanece oculta/bloqueada antes de autorizar una prueba sintetica real.
