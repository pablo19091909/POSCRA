# Fase 4A - Auditoria del flujo actual de ventas

Fecha UTC: 2026-06-26 10:45:52 UTC

## Alcance

Auditoria de solo lectura del flujo actual de registro de ventas antes de disenar e implementar una API transaccional. No se creo endpoint de ventas, no se modifico WPF, no se modifico base de datos y no se registraron ventas.

## Flujo principal encontrado

Archivo principal: `PulperiaPOS/VentasPage.xaml.cs`.

Flujo textual actual:

```text
Usuario selecciona cliente
-> WPF carga o busca productos
-> WPF agrega items al carrito local
-> WPF calcula subtotal y total
-> WPF valida metodo de pago
-> WPF confirma con MessageBox
-> WPF abre conexion SQL directa
-> WPF inicia transaccion SQL
-> INSERT ventas
-> por cada producto: INSERT DetalleVenta
-> por cada producto: UPDATE inventario stock/vendido
-> si cliente no es Cliente General: UPDATE cliente saldo
-> COMMIT
-> abre caja si efectivo
-> imprime recibo opcional
-> limpia formulario
```

## Pasos y efectos

| Paso | Archivo/metodo | Tabla | SQL | Columnas | Validaciones actuales | Efecto financiero | Riesgo |
|---|---|---|---|---|---|---|---|
| Agregar producto | `VentasPage.AgregarProductoPorCodigo` / `AgregarProducto_Click` | `inventario` | SELECT | `idProducto`, `nombre`, `precio`, `stock` | texto no vacio, stock mayor que cero | define precio y stock local del carrito | precio/stock vienen del WPF al momento de pagar |
| Editar cantidad | `ProductosDataGrid_CellEditEnding` | ninguna | ninguna | `Cantidad` local | cantidad mayor a cero, no mayor a `StockDisponible` local | cambia total local | puede quedar obsoleto frente a otra venta concurrente |
| Calcular total | `ActualizarTotal`, `Pagar_Click` | ninguna | ninguna | `ProductoVenta.Subtotal` | suma local | total financiero nace en WPF | WPF puede ser fuente no confiable |
| Efectivo | `Pagar_Click`, `CalcularVuelto` | `ventas` | INSERT | `monto_pagado`, `vuelto`, `metodo_pago` | monto pagado >= total | aumenta efectivo segun calculo de caja | no hay registro de movimiento de caja por venta |
| Dolares | `Pagar_Click`, `CalcularVuelto`, `TipoCambioHelper` | `ventas`, `TipoCambioDolar` | SELECT/INSERT/UPDATE tipo cambio, INSERT venta | `monto_pagado`, `vuelto` en colones | tipo cambio del dia obligatorio | efectivo equivalente en colones | no se guarda monto original en dolares ni tipo aplicado |
| Tarjeta | `Pagar_Click` | `ventas` | INSERT | `numero_voucher` | voucher requerido | no aumenta efectivo fisico | sin entidad de pago separada |
| SINPE | `Pagar_Click` | `ventas` | INSERT | `numero_comprobante` | comprobante requerido | no aumenta efectivo fisico | sin conciliacion separada |
| Saldo cliente | `ActualizarVisualSaldoCliente`, `DescontarSaldoCliente` | `cliente`, `ventas` | SELECT/UPDATE/INSERT | `saldo`, `metodo_pago` | saldo visual >= total | descuenta saldo cliente | update no condicionado puede dejar saldo negativo |
| Encabezado venta | `Pagar_Click` | `ventas` | INSERT | `cliente_id`, `total`, `fecha`, `hora`, `usuario_id`, pagos | `ValidarVenta` basico | crea factura | muchos campos nullable, total viene de WPF |
| Detalle | `InsertarDetalleVenta` | `DetalleVenta` | INSERT | `factura`, `producto_id`, `cantidad`, `precio_unitario` | ninguna adicional | registra detalle | precio unitario viene del WPF |
| Stock | `ActualizarStockProducto` | `inventario` | UPDATE | `stock`, `vendido` | ninguna condicion SQL | descuenta inventario | puede generar stock negativo por concurrencia |
| Recibo | `RawPrinterHelper.ImprimirReciboPOS58` | ninguna | ninguna | recibo impreso | checkbox opcional | comprobante fisico | ocurre despues del commit |
| Caja | `AbrirCajaDesdePOS58`, `CajaHelper` | `ventas`, `ingreso_caja`, `retiro_caja`, `cierre_caja` | SELECT/INSERT en otros flujos | varias | no bloquea venta despues de cierre | caja calcula desde ventas | ventas no generan movimiento de caja atomicamente |

## Metodos de pago encontrados

- `Efectivo`
- `Tarjeta`
- `Sinpe`
- `Dolares`
- `Saldo Cliente`

No se encontro soporte real para pagos combinados dentro de `VentasPage`.

## Edicion y eliminacion posterior

Archivo: `PulperiaPOS/Views/VentasCrudWindow.xaml.cs`.

Riesgos encontrados:

- `AgregarVenta_Click` inserta una venta dummy con total cero.
- `ActualizarVenta_Click` aumenta `ventas.total` en 100 sin recalcular detalle.
- `EliminarVenta_Click` borra fisicamente `DetalleVenta` y `ventas`, revierte stock y devuelve saldo al cliente si aplica.
- `BtnReimprimir_Click` reimprime factura desde venta historica.

Estas operaciones afectan integridad financiera e historica y deben redisenarse antes de migrar ventas.

## Relacion con caja

La venta no inserta en `ingreso_caja`, `retiro_caja` ni `cierre_caja`. Caja calcula:

- efectivo desde `ventas` con `metodo_pago='efectivo'`;
- ingresos manuales desde `ingreso_caja`;
- retiros desde `retiro_caja`;
- SINPE de clientes desde saldos cargados a cliente;
- cierre desde agregados de ventas e ingresos/retiros.

Esto debe esperar a la futura fase `CajaTurno` y `MovimientoCaja`.

## Observaciones de integridad agregada

Conteos agregados observados durante la auditoria:

- `ventas`: 1881 registros.
- `DetalleVenta`: 4921 registros.
- `inventario`: 220 productos.
- `cliente`: 162 clientes.
- `ingreso_caja`: 9 registros.
- `retiro_caja`: 6 registros.
- `cierre_caja`: 15 registros.

No se listaron clientes, productos, ventas, precios, comprobantes ni datos sensibles.
