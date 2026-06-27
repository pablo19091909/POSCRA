# Diseno futuro de venta transaccional por API

Fecha UTC: 2026-06-26 10:45:52 UTC

## Principios

- WPF envia intencion de venta, no totales finales.
- API recalcula precios, subtotales, total, stock, saldo y vuelto.
- API valida permisos, usuario activo e idempotencia.
- API ejecuta una sola transaccion SQL.
- Si cualquier paso falla, todo hace rollback.
- No implementar aun `POST /api/ventas`.

## Flujo propuesto

```text
POST /api/ventas
-> Validar JWT
-> Validar permiso Ventas.Crear
-> Validar usuario activo
-> Validar IdempotencyKey
-> Validar payload
-> Validar cliente
-> Cargar productos desde inventario con lock transaccional
-> Recalcular precios desde servidor
-> Validar stock con cantidades solicitadas
-> Validar pagos y vuelto
-> Validar saldo cliente si aplica
-> Crear encabezado venta
-> Crear detalles
-> Descontar stock con condicion stock >= cantidad
-> Actualizar vendido
-> Descontar saldo con condicion saldo >= monto
-> Registrar pagos
-> Registrar movimiento de caja si corresponde o dejar pendiente a CajaTurno
-> Registrar auditoria
-> Guardar idempotencia
-> Commit
-> Responder factura y resumen seguro
```

## Contratos propuestos

### CrearVentaRequest

```json
{
  "idempotencyKey": "uuid",
  "clienteId": 0,
  "items": [],
  "pagos": [],
  "observaciones": "",
  "cajaTurnoId": null
}
```

Campos:

- `idempotencyKey`: obligatorio.
- `clienteId`: obligatorio; puede representar Cliente General si se define regla explicita.
- `items`: uno o mas productos.
- `pagos`: uno o mas pagos cuando se habiliten combinaciones.
- `observaciones`: opcional.
- `cajaTurnoId`: futuro, nullable hasta fase CajaTurno.

### VentaItemRequest

```json
{
  "productoId": "codigo",
  "cantidad": 1
}
```

Reglas:

- `productoId` obligatorio.
- `cantidad` entera mayor que cero.
- No enviar precio como fuente final.

### PagoVentaRequest

```json
{
  "metodo": "Efectivo",
  "montoRecibido": 0,
  "referencia": "",
  "moneda": "CRC",
  "tipoCambio": null
}
```

Reglas:

- `metodo`: `Efectivo`, `Tarjeta`, `Sinpe`, `Dolares`, `SaldoCliente`.
- `montoRecibido`: obligatorio para efectivo/dolares; para otros metodos segun regla.
- `referencia`: voucher o comprobante cuando aplique.
- `moneda`: `CRC` o `USD`.
- `tipoCambio`: API debe usar el vigente y devolver el aplicado; no confiar en WPF.

### VentaResponse

```json
{
  "factura": 0,
  "fechaHoraUtc": "2026-01-01T00:00:00Z",
  "clienteId": 0,
  "total": 0,
  "montoPagado": 0,
  "vuelto": 0,
  "items": [],
  "pagos": [],
  "estado": "Registrada"
}
```

### VentaErrorResponse

```json
{
  "traceId": "",
  "codigo": "StockInsuficiente",
  "mensaje": "No se pudo registrar la venta.",
  "errores": []
}
```

Codigos sugeridos:

- `VentaInvalida`
- `StockInsuficiente`
- `SaldoInsuficiente`
- `ProductoNoDisponible`
- `ClienteInvalido`
- `PagoInvalido`
- `IdempotencyKeyRepetida`
- `CajaCerrada`
- `PermisoInsuficiente`

## Reglas por metodo de pago

| Metodo | Regla futura |
|---|---|
| Efectivo | API calcula vuelto; afecta caja fisica o MovimientoCaja futuro. |
| Tarjeta | Requiere referencia/voucher; no aumenta efectivo fisico. |
| SINPE | Requiere comprobante; no aumenta efectivo fisico. |
| Dolares | API toma tipo de cambio vigente, guarda monto USD y CRC aplicado. |
| SaldoCliente | API descuenta con `UPDATE cliente SET saldo = saldo - @monto WHERE idCliente = @id AND saldo >= @monto`. |

## Actualizaciones condicionadas

Stock:

```sql
UPDATE inventario
SET stock = stock - @cantidad,
    vendido = ISNULL(vendido, 0) + @cantidad
WHERE idProducto = @productoId
  AND ISNULL(stock, 0) >= @cantidad;
```

Saldo:

```sql
UPDATE cliente
SET saldo = saldo - @monto
WHERE idCliente = @clienteId
  AND ISNULL(saldo, 0) >= @monto;
```

## Tablas futuras recomendadas

- `venta_pago`
- `venta_auditoria`
- `venta_idempotencia`
- `caja_turno`
- `movimiento_caja`
- `venta_anulacion`
- `venta_devolucion`

## Pendiente para fases posteriores

- Disenar `CajaTurno` y `MovimientoCaja`.
- Sustituir borrado fisico por anulacion.
- Definir devoluciones con stock y caja.
- Migrar reportes a modelos consistentes.
