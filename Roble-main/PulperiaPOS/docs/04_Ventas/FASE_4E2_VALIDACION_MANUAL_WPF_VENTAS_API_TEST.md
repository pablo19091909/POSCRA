# Fase 4E.2 - Validacion manual WPF Ventas API en Test

Fecha/hora de cierre: 2026-06-28 11:08 UTC.

## Alcance

Se cierra formalmente la validacion manual de `VentasPage` contra `POST /api/ventas` usando ambiente marcado como Test. No se ejecutaron ventas nuevas durante este cierre; la revision posterior se hizo con consultas agregadas, inspeccion de codigo, compilacion y health checks.

## Ambiente

- Base confirmada con marca `Environment=Test`.
- Datos de prueba identificados por clientes/productos `API_TEST_` o por marca tecnica de idempotencia de Venta API.
- Ruta WPF API validada previamente con `UseVentasApiWrite=true` y ruta SQL preservada con `UseVentasApiWrite=false`.
- Estado final seguro restaurado:
  - WPF `FeatureFlags:UseVentasApiWrite=false`.
  - POS.Api `FeatureFlags:EnableVentasApiWrite=false`.

## Casos funcionales validados manualmente

- Efectivo exacto.
- Efectivo con vuelto.
- Tarjeta.
- SINPE.
- Saldo Cliente.
- Error por stock insuficiente.
- Error por saldo insuficiente.
- Cliente General.
- Recibo posterior a exito API.
- Ausencia de fallback automatico a SQL ante errores API.

## Integridad agregada

La ruta API se midio usando `venta_idempotencia` como marcador tecnico, sin exponer facturas, clientes, productos, precios ni saldos individuales.

| Indicador | Resultado |
| --- | ---: |
| Ventas API completadas | 10 |
| Detalles API relacionados | 10 |
| Pagos API relacionados | 10 |
| Auditorias API relacionadas | 10 |
| Idempotencias completadas | 10 |
| Ventas API sin detalle | 0 |
| Ventas API sin pago | 0 |
| Ventas API sin auditoria `VentaCreada` | 0 |
| Detalles sin encabezado | 0 |
| Pagos sin venta | 0 |
| Auditorias sin venta | 0 |
| Facturas con pago multiple | 0 |
| Facturas con idempotencia multiple | 0 |
| Pagos con monto no positivo | 0 |
| Efectivo con vuelto invalido | 0 |
| Metodo no permitido V1 | 0 |
| Dolares o Donacion por API | 0 |
| Stock negativo en productos sinteticos | 0 |
| Saldo negativo en clientes sinteticos | 0 |

## Cliente General

- Existe regla explicita para permitir Cliente General con identificador `0`.
- WPF ya no rechaza Cliente General por identificador `0`.
- POS.Api valida `ClienteId < 0` como invalido, por lo que `0` queda permitido para Cliente General.
- Cliente General aparece primero en el selector para evitar precargar un cliente historico.
- Cliente General no usa metodo `SaldoCliente` en Venta API.
- No se desconto saldo de clientes reales por ventas de Cliente General.

## Caja

La linea base de caja se mantuvo sin cambios:

| Agregado | Resultado |
| --- | ---: |
| ingreso_caja | 9 |
| retiro_caja | 6 |
| cierre_caja | 15 |

No hubo movimientos nuevos de caja por ventas API. Esto es esperado porque CajaTurno y MovimientoCaja quedan pendientes para una fase posterior; el recibo de Venta API no debe interpretarse todavia como movimiento auditable de caja.

## Ruta API y no fallback

Con `UseVentasApiWrite=true`, `VentasPage` llama `PagarConApiAsync()` y usa `VentasApiClient` hacia `api/ventas`. La ruta SQL historica queda separada en `PagarConSql()` y se conserva para `UseVentasApiWrite=false`.

No se encontro fallback automatico a SQL ante errores de Venta API.

## Validacion tecnica final

- Solucion completa compilada: 0 errores.
- WPF compila: 0 errores.
- POS.Api compila: 0 errores.
- Health checks:
  - `/health`: HTTP 200.
  - `/health/database`: HTTP 200.
  - `/api/system/version`: HTTP 200.
- `POST /api/ventas` sin token: HTTP 401 seguro.
- La validacion autenticada de flag apagado para HTTP 503 no se repitio en este cierre para no solicitar, leer ni persistir credenciales o JWT. El flag quedo confirmado en `false` en configuracion local y versionada.

## Limitaciones vigentes

- Dolares bloqueado en Venta API V1.
- Donacion bloqueada en Venta API V1.
- Pagos combinados no soportados.
- Sin CajaTurno ni MovimientoCaja.
- Sin anulacion ni devolucion API.
- Sin activacion permanente de escritura API.

## Recomendacion

Avanzar a Fase 4F para disenar e implementar la integracion de caja operacional de ventas API, manteniendo la escritura API apagada por defecto hasta contar con CajaTurno, MovimientoCaja y criterios de auditoria definidos.
