# Riesgos criticos del flujo actual de venta

Fecha UTC: 2026-06-26 10:45:52 UTC

## Criticos

1. SQL directo en operacion financiera desde WPF.
   - Archivo: `VentasPage.xaml.cs`.
   - Riesgo: el cliente de escritorio es autoridad de precio, total, stock y saldo.

2. Ausencia de idempotencia.
   - Archivo: `VentasPage.xaml.cs`, metodo `Pagar_Click`.
   - Riesgo: doble clic o reintento de red puede duplicar ventas.

3. Stock puede quedar negativo por concurrencia.
   - Metodo: `ActualizarStockProducto`.
   - SQL actual: descuenta sin condicion `stock >= cantidad`.
   - Riesgo: dos ventas simultaneas pueden vender el ultimo producto.

4. Saldo cliente puede quedar negativo.
   - Metodo: `DescontarSaldoCliente`.
   - SQL actual: descuenta sin condicion `saldo >= monto`.
   - Evidencia agregada: saldo total de clientes observado en negativo.

5. WPF calcula y envia total final.
   - Metodo: `Pagar_Click`.
   - Riesgo: precio o total no se recalcula del lado servidor.

6. Precio unitario viene del carrito WPF.
   - Metodo: `InsertarDetalleVenta`.
   - Riesgo: precio historico puede no coincidir con inventario o puede ser manipulado.

7. Edicion historica de ventas.
   - Archivo: `VentasCrudWindow.xaml.cs`.
   - Metodo: `ActualizarVenta_Click`.
   - Riesgo: aumenta total sin detalle ni auditoria.

8. Borrado fisico de ventas.
   - Archivo: `VentasCrudWindow.xaml.cs`.
   - Metodo: `EliminarVenta_Click`.
   - Riesgo: elimina encabezado y detalle; afecta stock y saldo sin conservar auditoria.

9. Caja inconsistente.
   - Archivos: `VentasPage.xaml.cs`, `CajaHelper.cs`, `CierreCajaPage.xaml.cs`.
   - Riesgo: venta no genera movimiento de caja atomico; caja se calcula por agregados.

10. Ausencia de auditoria transaccional.
    - Riesgo: no hay registro robusto de quien creo, anulo, modifico o reintento una venta.

## Altos

1. Campos financieros nullable en `ventas`.
   - `total`, `fecha`, `hora`, `usuario_id`, `metodo_pago`, `monto_pagado`, `vuelto` permiten nulos.

2. Metodo dolares no guarda monto original ni tipo de cambio usado.
   - Riesgo: no se puede auditar conversion.

3. Cierre de caja no bloquea ventas posteriores.
   - Riesgo: ventas posteriores pueden caer en periodo cerrado segun calculos por hora.

4. Usuario de retiro no queda en tabla `retiro_caja`.
   - Riesgo: trazabilidad incompleta.

5. Venta dummy y actualizacion manual desde CRUD.
   - Riesgo: datos artificiales o totales sin detalle.

## Medios

1. Uso de `double`/`float` en logica financiera WPF.
   - Riesgo: redondeos inconsistentes.

2. Errores tecnicos se muestran al usuario.
   - Riesgo: exposicion de detalles internos.

3. Reimpresion usa datos actuales de inventario para nombres.
   - Riesgo: cambios posteriores de nombre pueden alterar recibo reimpreso.

4. `TipoCambioDolar` usa `real`.
   - Riesgo: precision insuficiente para dinero.

## Bajos

1. Recibo se imprime despues del commit.
   - Riesgo operativo: venta registrada aunque impresora falle.

2. Apertura de caja no forma parte de transaccion.
   - Riesgo operativo, no necesariamente de datos.

## Reglas minimas antes de migrar

- La API debe ser unica autoridad de precio, stock, saldo, total, vuelto y pagos.
- La operacion debe ser atomica con commit o rollback total.
- Debe existir idempotencia por solicitud.
- Las actualizaciones de stock y saldo deben ser condicionadas.
- No debe existir borrado fisico de ventas.
- La caja debe esperar a `CajaTurno` y `MovimientoCaja`.
