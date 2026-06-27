# Fase 4C.1 - Contratos y pipeline preparado para ventas API

Fecha/hora UTC: 2026-06-26

## Alcance

Se creo el contrato inicial y la estructura interna de `POST /api/ventas` en POS.Api.

La escritura permanece desactivada por `FeatureFlags:EnableVentasApiWrite=false`. Con el flag apagado, el endpoint protegido responde de forma segura y no ejecuta escrituras.

## Flujo futuro

```text
JWT valido
-> permiso Ventas.Crear
-> feature flag de escritura habilitado
-> validar request
-> validar idempotency key
-> abrir transaccion SQL
-> reservar idempotencia
-> cargar cliente y productos
-> recalcular precios y total
-> validar stock condicionado
-> validar saldo condicionado
-> insertar ventas
-> insertar DetalleVenta
-> insertar venta_pago
-> descontar inventario
-> descontar saldo si aplica
-> insertar venta_auditoria
-> marcar idempotencia completada
-> commit
-> responder factura y totales
```

## Comportamiento actual

Prioridad actual del endpoint:

1. Requiere JWT.
2. Requiere permiso `Ventas.Crear`.
3. Si el flag esta apagado, responde `503 Service Unavailable`.
4. No valida ni escribe datos de venta mientras el flag esta apagado.

Esta prioridad evita que solicitudes invalidas expongan reglas internas cuando la escritura no esta disponible.

## Validaciones preparadas

- `idempotencyKey` requerido y no vacio.
- `clienteId` positivo.
- Lista de items requerida.
- Limite de items por solicitud.
- `productoId` requerido y acotado.
- Cantidad entera positiva y con limite maximo.
- Productos duplicados rechazados en esta version.
- Observaciones, referencia y voucher con longitud limitada.
- Un solo pago por venta.
- Metodo de pago dentro del conjunto soportado.
- `Donación` excluido de la venta API inicial.
- Moneda limitada a `CRC` o `USD` cuando se envia.
- `Dolares` requiere tipo de cambio observado positivo.

## Validaciones futuras dentro de transaccion

Cuando se active escritura, deben validarse dentro de una unica transaccion:

- Usuario autenticado y permiso efectivo.
- Cliente existente y habilitado para la operacion.
- Productos existentes.
- Precio vigente desde servidor.
- Stock suficiente condicionado al inventario actual.
- Saldo suficiente si aplica `SaldoCliente`.
- Total recalculado por servidor.
- Monto recibido suficiente para efectivo.
- Vuelto calculado por servidor.
- Idempotencia reservada y consistente.

## Autoridad de datos

WPF no es autoridad para precio, stock, total, vuelto, usuario, fecha/hora, saldo ni permisos. Esos valores deben calcularse o validarse en POS.Api dentro de la transaccion futura.

## Doble clic y reintento de red

El doble clic y el reintento de red se resolveran con `usuario_id + idempotency_key`.

- Misma clave y mismo request: repetir respuesta segura si la venta ya fue completada.
- Misma clave y request distinto: conflicto.
- Solicitud en proceso: responder estado controlado.
- Solicitud fallida: responder segun si es recuperable.

## Fuera de alcance V1

- Pagos combinados.
- Donaciones.
- Anulaciones.
- Devoluciones.
- Edicion o borrado de ventas.
- CajaTurno y MovimientoCaja.
- Integracion WPF con escritura API.

## Relacion futura con caja

La venta API inicial registrara `venta_pago`. La relacion con `CajaTurno` y `MovimientoCaja` debe definirse antes de activar escritura productiva, para que caja y cierre no dependan solo de agregados historicos.

## Seguridad

No se registran tokens, connection strings, usuarios, hashes, datos de clientes, productos ni ventas en logs.

## Resultado esperado de Fase 4C.1

Endpoint existente y protegido, pero no operativo para escritura hasta una fase posterior.

## Validacion ejecutada

Compilacion:

- `PulperiaPOS`: 0 errores.
- `POS.Api`: 0 errores.

Health checks:

- `/health`: HTTP 200.
- `/health/database`: HTTP 200.
- `/api/system/version`: HTTP 200.

Pruebas `POST /api/ventas`:

| Caso | Resultado |
| --- | --- |
| Sin token | HTTP 401 |
| Token sin `Ventas.Crear` | HTTP 403 |
| Token con `Ventas.Crear` y flag apagado | HTTP 503 |
| Request invalido con flag apagado | HTTP 503 por prioridad de feature flag |

Integridad agregada antes y despues:

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

No se modificaron datos historicos, inventario, saldos, caja, cierre ni WPF.
