# Flujos Criticos Actuales

## A. Login

```text
Usuario abre LoginWindow
-> BtnIngresar_Click
-> ObtenerDatosUsuario(usuario, contrasena)
-> Seguridad.HashContrasena(contrasena) con SHA-256
-> SELECT idUsuario, rol, nombre FROM usuario
-> UserSession.IdUsuario/NombreUsuario/RolUsuario
-> abre VentanaAdministrador o VentanaAnfitrion
```

Evidencia:

- Entrada: `LoginWindow.xaml.cs`, `BtnIngresar_Click`.
- Hash: `Seguridad.cs`, `HashContrasena`.
- Tabla: `usuario`.
- Operacion: SELECT.

Validaciones actuales:

- Usuario y contrasena deben coincidir con hash guardado.
- Rol decide ventana.

Si falla:

- Se muestra error generico de usuario/password o `ex.Message`.
- No hay bloqueo, rate limit ni auditoria de intentos.

Riesgos:

- Hash SHA-256 simple sin sal.
- SQL y credenciales DB en cliente.
- Permiso depende de rol en UI.

Migrar primero:

- `POST /auth/login` con BCrypt/Argon2 compatible con hashes legados.

## B. Venta

```text
VentasPage
-> CargarClientes SELECT cliente
-> AgregarProductoPorCodigo SELECT inventario
-> Pagar_Click valida UI
-> BeginTransaction
-> INSERT ventas
-> por cada producto:
   -> INSERT DetalleVenta
   -> UPDATE inventario stock = stock - cantidad
-> si cliente != Cliente General:
   -> UPDATE cliente saldo = saldo - total
-> Commit
-> impresion/apertura caja
```

Evidencia:

- Entrada: `VentasPage.xaml.cs`, `Pagar_Click`.
- Tablas: `ventas`, `DetalleVenta`, `inventario`, `cliente`.
- Operaciones: SELECT, INSERT, UPDATE.
- Transaccion: `connection.BeginTransaction()`.

Validaciones actuales:

- Hay productos.
- Cliente y metodo de pago seleccionados.
- Efectivo: monto pagado >= total.
- Tarjeta: voucher no vacio.
- SINPE: comprobante no vacio.
- Dolares: tipo de cambio del dia existe y monto convertido >= total.
- Stock se revisa al agregar al carrito.

Si falla:

- Dentro del `try`, una excepcion antes de `Commit` deberia descartar la transaccion al disponer el objeto.
- No hay idempotencia persistida; doble clic o reintento puede crear venta duplicada.

Riesgos:

- Stock validado antes, pero UPDATE no exige `stock >= cantidad`.
- Saldo de cliente no se descuenta con condicion `saldo >= total`.
- Fecha/hora vienen del cliente con `DateTime.Now`.
- Total y reglas viven en WPF.
- Venta no crea movimiento de caja formal.

Migrar primero:

- `POST /ventas` server-side con transaccion unica, idempotencia, stock/saldo condicionados y MovimientoCaja.

## C. Actualizacion de stock

```text
VentasPage.ActualizarStockProducto
-> UPDATE inventario
   SET stock = stock - @cantidad, vendido = vendido + @cantidad
   WHERE idProducto = @producto_id
```

Otros flujos:

- `DonacionesPage.RegistrarDonacion_Click` descuenta stock.
- `VentasCrudWindow.EliminarVenta_Click` repone stock al borrar venta.
- `ProductoForm.BtnGuardar_Click` permite editar stock manualmente.

Validaciones actuales:

- En venta/donacion se valida stock al agregar producto.
- No hay condicion en UPDATE para evitar stock negativo bajo concurrencia.

Riesgos:

- Dos usuarios pueden vender el ultimo producto.
- Edicion manual de stock no deja movimiento de inventario.

Migrar primero:

- Servicio API de venta/donacion con `UPDATE ... WHERE stock >= @cantidad`.
- Tabla futura de movimientos de inventario.

## D. Venta con saldo de cliente

```text
Cliente seleccionado distinto de Cliente General
-> MetodoPagoComboBox se fuerza a "Saldo Cliente"
-> ObtenerSaldoCliente SELECT cliente.saldo
-> UI calcula saldo restante
-> Pagar_Click
-> INSERT ventas
-> INSERT DetalleVenta
-> UPDATE inventario
-> UPDATE cliente SET saldo = saldo - @monto
-> Commit
```

Evidencia:

- `VentasPage.xaml.cs`, `ClienteComboBox_SelectionChanged`.
- `VentasPage.xaml.cs`, `ObtenerSaldoCliente`.
- `VentasPage.xaml.cs`, `DescontarSaldoCliente`.
- Tabla: `cliente`.

Validaciones actuales:

- UI deshabilita pagar si saldo restante < 0.

Si falla:

- La transaccion de venta cubre descuento de saldo, pero no valida saldo suficiente al momento del UPDATE.

Riesgos:

- Saldo negativo por concurrencia.
- Metodo de pago "Saldo Cliente" no debe aumentar efectivo fisico, pero no hay MovimientoCaja que lo documente.

Migrar primero:

- API de venta con `UPDATE cliente SET saldo = saldo - @monto WHERE saldo >= @monto`.

## E. Ingreso de caja

```text
IngresoCajaPage
-> RegistrarIngreso_Click
-> valida monto numerico
-> fecha/hora DateTime.Now
-> usuario = UserSession.NombreUsuario
-> INSERT ingreso_caja
-> abre caja fisica
-> CargarIngresos SELECT ingreso_caja
-> CalcularDineroEnCaja CajaHelper.ObtenerDineroAcumuladoCajaChica
```

Evidencia:

- Entrada: `IngresoCajaPage.xaml.cs`, `RegistrarIngreso_Click`.
- Tabla: `ingreso_caja`.
- Operacion: INSERT.

Validaciones actuales:

- Monto debe parsear como `double`.
- Motivo no se valida como obligatorio en UI aunque la tabla lo define NOT NULL.

Si falla:

- Se muestra `ex.Message`.
- No hay transaccion ni MovimientoCaja.

Riesgos:

- Usuario guardado como texto, no FK a `usuario`.
- Fecha/hora de cliente.
- No hay turno/caja/cierre asociado.

Migrar primero:

- `POST /caja/ingresos` requiere turno abierto, usuario autenticado y MovimientoCaja.

## F. Retiro de caja

```text
RetirosCajaPage
-> CalcularDineroEnCaja
   -> CajaHelper.ObtenerDineroAcumuladoCajaChica
-> RegistrarRetiro_Click
-> valida monto
-> compara monto > dineroDisponibleEnCaja en UI
-> abre caja fisica
-> INSERT retiro_caja
-> imprime recibo
```

Evidencia:

- Entrada: `Views\RetirosCajaPage.xaml.cs`, `RegistrarRetiro_Click`.
- Tabla: `retiro_caja`.
- Operacion: INSERT.

Validaciones actuales:

- Monto numerico.
- Monto no mayor al disponible calculado antes.

Si falla:

- No hay transaccion con el calculo de disponible.
- Error se muestra al usuario.

Riesgos:

- Retiros concurrentes pueden exceder disponible.
- `retiro_caja` no guarda usuario en base; solo se imprime usuario.
- No hay turno/caja/cierre.

Migrar primero:

- `POST /caja/retiros` con validacion de disponible en la misma transaccion.

## G. Cierre de caja

```text
CierreCajaPage
-> CalcularTotalesDelDia
   -> CajaHelper.ObtenerTotalesCaja
   -> SELECT ventas agrupadas por metodo_pago
-> GuardarCierre_Click
-> abre caja fisica
-> fecha/hora DateTime.Now
-> INSERT cierre_caja(total_efectivo,total_sinpe,total_datafono,observaciones)
-> imprime cierre
```

Evidencia:

- Entrada: `Views\CierreCajaPage.xaml.cs`, `GuardarCierre_Click`.
- Calculos: `CajaHelper.cs`, `ObtenerTotalesCaja`.
- Tabla: `cierre_caja`.
- Operaciones: SELECT, INSERT.

Validaciones actuales:

- Confirmacion visual del usuario.
- No hay conteo fisico separado del esperado.

Si falla:

- Puede abrir la caja fisica antes de fallar el INSERT.
- No bloquea movimientos ya incluidos.

Riesgos:

- Cierre por fecha/hora, no por turno.
- No hay `IdCaja`, `IdTurno`, `EfectivoEsperado`, `EfectivoContado`, `Diferencia`.
- No hay relacion entre cierre y ventas/ingresos/retiros.
- Puede haber multiples cierres incompatibles en un mismo dia.

Migrar primero:

- Modelo `CajaTurno`, `MovimientoCaja`, `CierreCaja`.

## H. Reportes

Flujos actuales:

- Inventario PDF: `InventarioWindow.BtnExportarPDF_Click` -> SELECT `inventario` -> PDF local.
- Clientes con saldo: `ClientePage.BtnReporteClientesSaldo_Click` -> SELECT `cliente` -> TXT en escritorio.
- Caja/cierre: `CierreCajaPage` y `CajaHelper` calculan totales con SELECT SUM.
- Reimpresion de venta: `VentasCrudWindow.BtnReimprimir_Click` -> SELECT detalle venta.

Riesgos:

- Reportes se calculan desde cliente.
- No hay snapshot por cierre/turno.
- Consultas no paginadas y con `SELECT *` en algunos casos.

Migrar primero:

- Reportes financieros despues de migrar caja/venta a MovimientoCaja.

## I. Edicion o eliminacion de ventas

```text
VentasCrudWindow
-> AgregarVenta_Click INSERT venta con total 0
-> ActualizarVenta_Click UPDATE ventas SET total = total + 100
-> EliminarVenta_Click
   -> BeginTransaction
   -> SELECT venta
   -> SELECT DetalleVenta
   -> UPDATE inventario repone stock
   -> DELETE DetalleVenta
   -> DELETE ventas
   -> UPDATE cliente saldo + total si aplica
   -> Commit
```

Evidencia:

- Archivo: `Views\VentasCrudWindow.xaml.cs`.
- Tablas: `ventas`, `DetalleVenta`, `inventario`, `cliente`.
- Operaciones: INSERT, UPDATE, DELETE.

Validaciones actuales:

- Seleccion de fila.
- Confirmaciones UI parciales.

Si falla:

- La eliminacion usa transaccion.
- No hay registro auditable de anulacion.

Riesgos:

- Borrado fisico de ventas y detalle.
- Edicion arbitraria de total.
- Crea ventas cero.
- Puede afectar caja/cierres historicos.

Migrar primero:

- Desactivar en produccion y reemplazar por `POST /ventas/{id}/anular` y `POST /devoluciones`.

## J. Cambio de tipo de cambio

```text
TipoCambioWindow
-> Guardar_Click
-> TipoCambioHelper.GuardarTipoCambio
-> IF EXISTS TipoCambioDolar fecha
   -> UPDATE compra/venta
   ELSE INSERT compra/venta
```

Evidencia:

- `TipoCambioWindow.xaml.cs`.
- `TipoCambioHelper.cs`, `GuardarTipoCambio`.
- Tabla: `TipoCambioDolar`.

Validaciones actuales:

- UI parsea compra/venta como numeros.
- Venta en dolares exige tipo de cambio del dia.

Si falla:

- Excepcion visible al usuario.

Riesgos:

- Tipo de cambio usa `real` en DB.
- No guarda usuario, fecha de modificacion ni historial.
- Venta en dolares no guarda tipo de cambio usado en la venta.

Migrar primero:

- API de tipo cambio con auditoria e incluir tasa usada en cada venta.
