# Contratos POST /api/ventas

## Endpoint

`POST /api/ventas`

Requiere:

- JWT valido.
- Permiso `Ventas.Crear`.
- `FeatureFlags:EnableVentasApiWrite=true` para escritura futura.

En Fase 4C.1 el flag permanece `false`.

## `CrearVentaRequest`

Campos:

- `clienteId`
- `items`
- `pago`
- `idempotencyKey`
- `observaciones`
- `tipoCambioObservado`
- `referenciaPago`
- `voucher`

No acepta como autoridad:

- Factura.
- Usuario.
- Fecha/hora oficial.
- Precio unitario final.
- Subtotal final.
- Total final.
- Stock.
- Vuelto final.
- Estado financiero.
- Estado de caja.
- Permisos.

## `VentaItemRequest`

Campos:

- `productoId`
- `cantidad`

Reglas:

- `cantidad` debe ser entero positivo.
- No se aceptan items duplicados en V1.
- No se acepta precio ni stock desde WPF.

## `PagoVentaRequest`

Campos:

- `metodoPago`
- `montoRecibido`
- `referencia`
- `voucher`
- `moneda`
- `tipoCambioObservado`

Reglas:

- Un solo pago por venta.
- No hay lista de pagos.
- No hay pago combinado.
- `Donación` no se procesa en V1.

## Respuestas preparadas

- `VentaResponse`
- `VentaItemResponse`
- `PagoVentaResponse`
- `VentaErrorResponse`

La respuesta futura puede incluir factura, estado, total recalculado, monto pagado, vuelto, metodo de pago, fecha UTC, resultado de idempotencia y detalles calculados por servidor.

No debe incluir secretos, SQL, stack traces, hashes ni informacion innecesaria de otros clientes o productos.
