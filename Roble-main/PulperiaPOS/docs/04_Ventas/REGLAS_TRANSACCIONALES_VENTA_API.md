# Reglas transaccionales venta API

## Principio

Una venta API debe completarse totalmente o no registrar nada. La operacion usa una unica transaccion SQL.

## Autoridad del servidor

POS.Api es autoridad para:

- Usuario.
- Fecha/hora UTC.
- Factura.
- Precio vigente.
- Total.
- Stock.
- Saldo.
- Pago.
- Vuelto.
- Auditoria.

WPF no es autoridad para esos valores.

## Stock

Por cada producto:

1. Validar existencia.
2. Obtener precio desde `inventario`.
3. Insertar detalle con precio servidor.
4. Descontar stock con condicion:

```sql
UPDATE dbo.inventario
SET stock = stock - @cantidad,
    vendido = ISNULL(vendido, 0) + @cantidad
WHERE idProducto = @producto_id
  AND ISNULL(stock, 0) >= @cantidad;
```

Si la actualizacion afecta 0 filas, la transaccion se revierte.

## Saldo cliente

Solo `SaldoCliente` descuenta saldo.

```sql
UPDATE dbo.cliente
SET saldo = saldo - @total
WHERE idCliente = @cliente_id
  AND saldo >= @total;
```

Si la actualizacion afecta 0 filas, la transaccion se revierte.

## Venta y detalle

La factura se genera por identity en `ventas.factura`.

`DetalleVenta` se inserta despues del encabezado y antes de confirmar pago/auditoria.

## Pago

Se inserta exactamente un registro en `venta_pago`.

No hay pagos combinados en V1.

## Auditoria

Se inserta evento `VentaCreada` en `venta_auditoria` dentro de la misma transaccion.

## Rollback

Ante error controlado o inesperado:

- Se revierte la transaccion.
- No debe quedar venta parcial.
- No debe quedar detalle parcial.
- No debe quedar stock o saldo parcialmente modificado.

Con flag apagado, nada de esta secuencia se ejecuta.
