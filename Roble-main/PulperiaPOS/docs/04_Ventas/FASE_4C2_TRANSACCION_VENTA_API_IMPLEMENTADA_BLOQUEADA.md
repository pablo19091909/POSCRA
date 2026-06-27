# Fase 4C.2 - Transaccion interna venta API implementada y bloqueada

Fecha/hora UTC: 2026-06-26

## Alcance

Se implemento la transaccion interna real para crear ventas desde POS.Api, manteniendo `FeatureFlags:EnableVentasApiWrite=false`.

Con el flag apagado, `POST /api/ventas` corta antes de llamar al repositorio transaccional y responde HTTP 503 seguro.

## Componentes implementados

- `VentaRepository.CreateVentaTransactionalAsync`.
- `CrearVentaPreparedCommand`.
- `VentaBusinessException`.
- `VentaPaymentCalculation`.
- `VentaTransactionItem`.
- Estados ampliados en `VentaServiceResult`.

## Secuencia transaccional preparada

La operacion futura usa una sola conexion y una sola transaccion SQL con aislamiento `Serializable`.

Secuencia:

1. Buscar idempotencia con `UPDLOCK, HOLDLOCK`.
2. Resolver solicitud repetida, en proceso, fallida o conflictiva.
3. Reservar idempotencia `EnProceso`.
4. Validar usuario activo.
5. Validar cliente existente.
6. Cargar productos con bloqueo de actualizacion.
7. Recalcular precios y total desde servidor.
8. Calcular pago y vuelto desde servidor.
9. Insertar `ventas`.
10. Insertar `DetalleVenta`.
11. Descontar stock con `UPDATE` condicionado.
12. Descontar saldo con `UPDATE` condicionado si aplica `SaldoCliente`.
13. Insertar `venta_pago`.
14. Insertar `venta_auditoria`.
15. Marcar idempotencia como `Completada`.
16. Confirmar transaccion.

Si ocurre un error controlado o inesperado, se ejecuta rollback.

## Escritura bloqueada

No se activo `EnableVentasApiWrite`.

Mientras el flag siga en `false`, no se ejecuta:

- Reserva de idempotencia.
- Insercion de venta.
- Insercion de detalle.
- Descuento de stock.
- Descuento de saldo.
- Insercion de pago.
- Insercion de auditoria.
- Generacion de factura.

## Esquema real revisado

Se revisaron metadatos de:

- `ventas`.
- `DetalleVenta`.
- `inventario`.
- `cliente`.
- `usuario`.
- `venta_idempotencia`.
- `venta_pago`.
- `venta_auditoria`.
- `TipoCambioDolar`.

Hallazgos aplicados:

- `ventas.factura` es identity `int`.
- `ventas` guarda `fecha` y `hora`.
- `DetalleVenta` usa `factura`, `producto_id`, `cantidad`, `precio_unitario`.
- `inventario` usa `precio`, `stock`, `vendido`.
- `cliente.saldo` es `decimal`.
- `TipoCambioDolar` guarda `real`; por eso `Dolares` queda no habilitado para escritura API V1 hasta definir fuente decimal server-side.

## Validacion final pendiente de esta fase

Resultado de pruebas no destructivas:

- Compilacion completa: 0 errores.
- `PulperiaPOS`: 0 errores.
- `POS.Api`: 0 errores.
- `/health`: HTTP 200.
- `/health/database`: HTTP 200.
- `/api/system/version`: HTTP 200.
- `POST /api/ventas` sin token: HTTP 401.
- `POST /api/ventas` con token sin `Ventas.Crear`: HTTP 403.
- `POST /api/ventas` con `Ventas.Crear` y flag apagado: HTTP 503.
- Tokens temporales usados solo en memoria y no impresos.

Conteos agregados antes y despues:

| Metrica | Antes | Despues |
| --- | ---: | ---: |
| Ventas | 1881 | 1881 |
| DetalleVenta | 4921 | 4921 |
| Inventario registros | 220 | 220 |
| Stock agregado | 2980 | 2980 |
| Clientes | 162 | 162 |
| Ingresos caja | 9 | 9 |
| Retiros caja | 6 | 6 |
| Cierres caja | 15 | 15 |
| `venta_idempotencia` | 0 | 0 |
| `venta_pago` | 0 | 0 |
| `venta_auditoria` | 0 | 0 |

La instancia local de API fue cerrada y el puerto `7046` quedo libre.

## Riesgos pendientes

- La transaccion no ha sido probada con escritura real.
- Falta ambiente controlado con datos de prueba para activar temporalmente el flag.
- Dolares requiere decision de tipo de cambio decimal.
- CajaTurno y MovimientoCaja siguen fuera de alcance.

## Siguiente paso recomendado

Fase 4C.3: pruebas controladas en ambiente no productivo con `EnableVentasApiWrite=true`, usando datos de prueba y rollback operativo definido antes de conectar WPF.
