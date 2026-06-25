# Matriz de Modulos y SQL Directo

## 1. Resumen

Todos los modulos funcionales revisados usan SQL directo desde WPF mediante `DBConnection.GetConnection()` o reciben `SqlConnection`/`SqlTransaction` creadas por la ventana. No se encontro uso de API o servicios externos HTTP.

## 2. Matriz principal

| Modulo | Pantalla/interfaz | Clases de logica | Tablas SQL | SQL directo | DBConnection | API externa | Operaciones | Riesgo | Prioridad migracion |
|---|---|---|---|---|---|---|---|---|---|
| Login | `LoginWindow.xaml` | `LoginWindow`, `Seguridad`, `UserSession` | `usuario` | Si | Si | No | SELECT usuario por nombre/hash | Critico: autenticacion en cliente, SHA-256 simple | Critica |
| Roles y permisos | `VentanaAdministrador.xaml`, `VentanaAnfitrion.xaml` | `UserSession`, ventanas de rol | `usuario` indirecta | Parcial | No directo en ventanas | No | Navegacion por rol y ocultamiento UI | Alto: permisos no server-side | Alta |
| Usuarios | `VentanaUsuarios.xaml`, `VentanaEditarUsuario.xaml` | `VentanaUsuarios`, `VentanaEditarUsuario`, `Seguridad` | `usuario` | Si | Si | No | SELECT, INSERT, UPDATE, DELETE | Critico: gestion directa de usuarios/hashes | Alta |
| Productos | `ProductoForm.xaml` | `ProductoForm` | `inventario` | Si | Si | No | SELECT COUNT, INSERT, UPDATE | Alto: precios/stock desde cliente | Alta |
| Inventario | `InventarioWindow.xaml` | `InventarioWindow` | `inventario` | Si | Si | No | SELECT, DELETE, PDF | Alto: borrado fisico de productos | Alta |
| Clientes | `ClientePage.xaml`, `ClienteForm.xaml` | `ClientePage`, `ClienteForm` | `cliente`, `ventas`, `saldo_liberado` | Si | Si | No | SELECT, INSERT, UPDATE, DELETE | Alto: saldo y clientes desde cliente | Alta |
| Saldo clientes | `ClientePage.xaml`, `SaldoLiberadoPage.xaml` | `ClientePage`, `SaldoLiberadoPage` | `cliente`, `saldo_liberado` | Si | Si | No | SELECT, INSERT, UPDATE | Alto: liberacion de saldo sin aprobacion formal | Alta |
| Ventas | `VentasPage.xaml` | `VentasPage`, `ProductoVenta` | `ventas`, `DetalleVenta`, `inventario`, `cliente` | Si | Si | No | SELECT, INSERT, UPDATE en transaccion | Critico: venta financiera desde WPF | Critica |
| Detalle ventas | `DetalleVentaWindow.xaml` | `DetalleVentaWindow` | `DetalleVenta`, `inventario` | Si | Si | No | SELECT | Medio: lectura directa | Media |
| Metodos de pago | `VentasPage.xaml` | `VentasPage` | `ventas`, `TipoCambioDolar`, `cliente` | Si | Si | No | Validacion UI, INSERT venta | Critico: reglas financieras en UI | Critica |
| Vuelto | `VentasPage.xaml` | `VentasPage.CalcularVuelto`, `Pagar_Click` | `ventas` | Si | Si | No | Calculo UI, INSERT `monto_pagado`/`vuelto` | Alto: calculo cliente | Alta |
| Tipo de cambio | `TipoCambioWindow.xaml` | `TipoCambioHelper` | `TipoCambioDolar` | Si | Si | No | SELECT, IF EXISTS UPDATE ELSE INSERT | Alto: tipo cambio no auditado por usuario | Alta |
| Ingreso caja | `IngresoCajaPage.xaml` | `IngresoCajaPage`, `CajaHelper` | `ingreso_caja`, `ventas`, `retiro_caja` | Si | Si | No | INSERT ingreso, SELECT historico | Critico: caja desde cliente | Critica |
| Retiro caja | `Views\RetirosCajaPage.xaml` | `RetirosCajaPage`, `CajaHelper` | `retiro_caja`, `ventas`, `ingreso_caja` | Si | Si | No | INSERT retiro, SELECT disponible | Critico: disponible no transaccional | Critica |
| Cierre caja | `Views\CierreCajaPage.xaml` | `CierreCajaPage`, `CajaHelper` | `cierre_caja`, `ventas`, `ingreso_caja`, `retiro_caja`, `cliente` | Si | Si | No | SELECT totales, INSERT cierre | Critico: sin turno/esperado/contado/diferencia formal | Critica |
| Reportes | `InventarioWindow`, `ClientePage`, `CajaHelper`, `CierreCajaPage` | Generadores PDF/texto/impresion | `inventario`, `cliente`, `ventas`, `cierre_caja` | Si | Si | No | SELECT, exportacion local, impresion | Medio: lectura directa y dependiente del cliente | Media |
| Donaciones | `DonacionesPage.xaml` | `DonacionesPage` | `ventas`, `DetalleVenta`, `inventario` | Si | Si | No | SELECT, INSERT, UPDATE stock en transaccion | Alto: stock sin condicion en UPDATE | Alta |
| Gestion ventas historicas | `Views\VentasCrudWindow.xaml` | `VentasCrudWindow` | `ventas`, `DetalleVenta`, `inventario`, `cliente` | Si | Si | No | SELECT, INSERT venta cero, UPDATE total, DELETE | Critico: manipulacion y borrado fisico | Critica |
| Anulacion/eliminacion | `Views\VentasCrudWindow.xaml`, `InventarioWindow`, `ClientePage`, `VentanaUsuarios` | Varias | `ventas`, `DetalleVenta`, `inventario`, `cliente`, `usuario` | Si | Si | No | DELETE fisico | Critico en ventas; alto en catalogos | Critica |
| Configuracion/credenciales | N/A | `DBConnection`, `App.config` | N/A | N/A | N/A | No | Connection string hardcodeado | Critico | Critica |

## 3. Hallazgos de acceso directo por archivo

| Archivo | Metodo | Tablas | Operacion | Riesgo | Accion futura |
|---|---|---|---|---|---|
| `DataAccess\DBConnection.cs` | `GetConnection`, `GetConnectionString` | N/A | Abre conexion Azure SQL | Critico | Migrar acceso DB a API y retirar del cliente. |
| `LoginWindow.xaml.cs` | `ObtenerDatosUsuario` | `usuario` | SELECT | Critico | `POST /auth/login`. |
| `VentanaUsuarios.xaml.cs` | `CargarUsuarios` | `usuario` | SELECT * | Alto | `GET /usuarios`. |
| `VentanaUsuarios.xaml.cs` | `BtnAgregar_Click` | `usuario` | INSERT | Alto | `POST /usuarios`. |
| `VentanaUsuarios.xaml.cs` | `BtnActualizar_Click` | `usuario` | UPDATE | Alto | `PUT /usuarios/{id}`. |
| `VentanaUsuarios.xaml.cs` | `BtnEliminar_Click` | `usuario` | DELETE | Alto | Desactivar delete fisico; usar estado. |
| `VentanaEditarUsuario.xaml.cs` | `BtnGuardar_Click` | `usuario` | SELECT COUNT | Medio | Validacion server-side. |
| `VentasPage.xaml.cs` | `CargarClientes` | `cliente` | SELECT | Medio | `GET /clientes`. |
| `VentasPage.xaml.cs` | `AgregarProductoPorCodigo` | `inventario` | SELECT | Alto | `GET /productos/buscar`. |
| `VentasPage.xaml.cs` | `Pagar_Click` | `ventas` | INSERT | Critico | `POST /ventas` transaccional/idempotente. |
| `VentasPage.xaml.cs` | `InsertarDetalleVenta` | `DetalleVenta` | INSERT | Critico | Dentro de servicio de venta. |
| `VentasPage.xaml.cs` | `ActualizarStockProducto` | `inventario` | UPDATE | Critico | UPDATE condicionado por stock en API. |
| `VentasPage.xaml.cs` | `DescontarSaldoCliente` | `cliente` | UPDATE | Critico | UPDATE condicionado por saldo en API. |
| `VentasPage.xaml.cs` | `ObtenerSaldoCliente` | `cliente` | SELECT | Alto | Consulta server-side dentro de venta/saldo. |
| `VentasPage.xaml.cs` | `ActualizarSaldoCliente` | `cliente` | UPDATE | Alto | Revisar uso y migrar a API. |
| `VentasPage.xaml.cs` | `BuscarProductoTextBox_TextChanged` | `inventario` | SELECT sugerencias | Medio | API de busqueda paginada. |
| `Views\VentasCrudWindow.xaml.cs` | `CargarVentas` | `ventas`, `cliente` | SELECT | Alto | `GET /ventas`. |
| `Views\VentasCrudWindow.xaml.cs` | `AgregarVenta_Click` | `ventas` | INSERT venta cero | Critico | Eliminar en produccion; reemplazar por flujo formal. |
| `Views\VentasCrudWindow.xaml.cs` | `ActualizarVenta_Click` | `ventas` | UPDATE total | Critico | Prohibir edicion directa; usar ajuste/anulacion. |
| `Views\VentasCrudWindow.xaml.cs` | `EliminarVenta_Click` | `ventas`, `DetalleVenta`, `inventario`, `cliente` | SELECT, UPDATE, DELETE | Critico | Reemplazar por anulacion auditable. |
| `Views\DetalleVentaWindow.xaml.cs` | `CargarDetalleVenta` | `DetalleVenta`, `inventario` | SELECT | Medio | `GET /ventas/{id}/detalle`. |
| `IngresoCajaPage.xaml.cs` | `RegistrarIngreso_Click` | `ingreso_caja` | INSERT | Critico | `POST /caja/ingresos`. |
| `IngresoCajaPage.xaml.cs` | `CargarIngresos` | `ingreso_caja` | SELECT * | Alto | `GET /caja/ingresos`. |
| `Views\RetirosCajaPage.xaml.cs` | `RegistrarRetiro_Click` | `retiro_caja` | INSERT | Critico | `POST /caja/retiros` con validacion transaccional. |
| `Views\RetirosCajaPage.xaml.cs` | `CargarRetiros` | `retiro_caja` | SELECT * | Alto | `GET /caja/retiros`. |
| `Views\CierreCajaPage.xaml.cs` | `CalcularTotalesDelDia` | `ventas` | SELECT agregado | Critico | Calculo por turno en API. |
| `Views\CierreCajaPage.xaml.cs` | `GuardarCierre_Click` | `cierre_caja` | INSERT | Critico | `POST /caja/turnos/{id}/cerrar`. |
| `Views\CierreCajaPage.xaml.cs` | `CargarCierresAnteriores` | `cierre_caja` | SELECT | Alto | `GET /caja/cierres`. |
| `CajaHelper.cs` | `ObtenerUltimaHoraDeCierreHoy` | `cierre_caja` | SELECT | Critico | Reemplazar por turno activo. |
| `CajaHelper.cs` | `ObtenerTotalesCaja` | `ventas`, `ingreso_caja`, `retiro_caja`, `cliente` | SELECT SUM | Critico | API calcula por `IdTurno`. |
| `CajaHelper.cs` | `ObtenerDineroAcumuladoCajaChica` | `ventas`, `ingreso_caja`, `retiro_caja` | SELECT SUM historico | Critico | MovimientoCaja por turno. |
| `InventarioWindow.xaml.cs` | `CargarInventario` | `inventario` | SELECT * | Medio | `GET /inventario`. |
| `InventarioWindow.xaml.cs` | `BtnEliminar_Click` | `inventario` | DELETE | Alto | Soft delete/estado. |
| `InventarioWindow.xaml.cs` | `BtnExportarPDF_Click` | `inventario` | SELECT * | Medio | Reporte desde API o vista. |
| `ProductoForm.xaml.cs` | `BtnGuardar_Click` | `inventario` | SELECT COUNT, INSERT, UPDATE | Alto | API con validaciones/auditoria. |
| `ClientePage.xaml.cs` | `CargarClientes` | `cliente` | SELECT * | Medio | `GET /clientes`. |
| `ClientePage.xaml.cs` | `BtnEliminar_Click` | `ventas`, `saldo_liberado`, `cliente` | SELECT COUNT, DELETE | Alto | Soft delete. |
| `ClientePage.xaml.cs` | `BtnLiberarSaldo_Click` | `saldo_liberado`, `cliente` | INSERT, UPDATE en transaccion | Alto | API con aprobacion/auditoria. |
| `ClientePage.xaml.cs` | `BtnReporteClientesSaldo_Click` | `cliente` | SELECT | Medio | Reporte API. |
| `ClienteForm.xaml.cs` | `BtnGuardar_Click` | `cliente` | INSERT, UPDATE | Alto | API con auditoria de saldo. |
| `SaldoLiberadoPage.xaml.cs` | `CargarLiberaciones` | `saldo_liberado`, `cliente` | SELECT | Medio | API de historial. |
| `DonacionesPage.xaml.cs` | `AgregarProductoPorCodigo` | `inventario` | SELECT | Alto | API busqueda. |
| `DonacionesPage.xaml.cs` | `RegistrarDonacion_Click` | `ventas`, `DetalleVenta`, `inventario` | INSERT, UPDATE en transaccion | Alto | `POST /donaciones` con stock condicionado. |
| `TipoCambioHelper.cs` | `ExisteTipoCambioParaHoy` | `TipoCambioDolar` | SELECT COUNT | Alto | API tipo cambio. |
| `TipoCambioHelper.cs` | `GuardarTipoCambio` | `TipoCambioDolar` | UPDATE/INSERT | Alto | API con auditoria. |
| `TipoCambioHelper.cs` | `ObtenerTipoCambioHoy` | `TipoCambioDolar` | SELECT | Alto | API tipo cambio vigente. |

## 4. Fallback SQL configurable

No se encontro fallback configurable. El acceso directo a SQL es el camino principal y unico observado para persistencia.

## 5. SQLite, LocalDB y archivos `.db`

| Elemento | Evidencia | Uso encontrado |
|---|---|---|
| `PulperiaPOS\Data\pulperia.db` | Archivo incluido como `None` en `.csproj` | No se encontro uso activo. |
| `System.Data.SQLite` | PackageReference en `.csproj` | No se encontro `SQLiteConnection` ni consultas SQLite. |
| LocalDB | Busqueda sin hallazgos activos | No encontrado. |

## 6. Prioridad futura de migracion

1. Login/usuarios/permisos y secretos.
2. Venta transaccional: venta, detalle, stock, saldo, pago, idempotencia.
3. Caja: ingreso, retiro, cierre por turno.
4. Anulacion/devoluciones y bloqueo de borrado fisico.
5. Inventario/clientes/tipo de cambio.
6. Reportes y consultas historicas.
