# Hallazgos Iniciales de Seguridad

## 1. Resumen

La aplicacion WPF actual contiene acceso directo a Azure SQL, autenticacion local contra tabla `usuario`, hash SHA-256 simple y permisos aplicados principalmente por navegacion/visibilidad de UI. No se encontro API, JWT ni autorizacion server-side en el alcance analizado.

## 2. Hallazgos priorizados

| ID | Prioridad | Hallazgo | Evidencia | Impacto | Recomendacion futura |
|---|---|---|---|---|---|
| SEG-001 | CRITICA | Connection string y credenciales de Azure SQL en codigo cliente | `PulperiaPOS\DataAccess\DBConnection.cs`, lineas 10-14 | Exposicion de base completa desde cliente o binario | Rotar credenciales y mover acceso DB a API con secretos por ambiente. |
| SEG-002 | CRITICA | SQL directo desde WPF en modulos financieros | `VentasPage.xaml.cs`, `IngresoCajaPage.xaml.cs`, `RetirosCajaPage.xaml.cs`, `CierreCajaPage.xaml.cs` | Manipulacion financiera posible desde cliente | Migrar a API con autorizacion server-side. |
| SEG-003 | ALTA | Passwords con SHA-256 simple sin sal | `Seguridad.cs`, `HashContrasena`; `LoginWindow.xaml.cs`, `ObtenerDatosUsuario` | Vulnerable a ataques offline si se filtra tabla | Migracion progresiva a BCrypt/Argon2. |
| SEG-004 | ALTA | No hay bloqueo/rate limiting de login | `LoginWindow.xaml.cs`, `BtnIngresar_Click` | Fuerza bruta | Implementar control server-side. |
| SEG-005 | CRITICA | Permisos por ventana/rol, no por accion server-side | `VentanaAdministrador.xaml.cs`, `VentanaAnfitrion.xaml.cs`, `ClientePage.xaml.cs`, `InventarioWindow.xaml.cs` | Usuarios o clientes modificados podrian ejecutar acciones no autorizadas | API con permisos: vender, retirar, cerrar, anular, usuarios, inventario. |
| SEG-006 | CRITICA | Borrado fisico de ventas | `Views\VentasCrudWindow.xaml.cs`, `EliminarVenta_Click` | Perdida de trazabilidad y posible alteracion de caja | Reemplazar por anulacion auditable. |
| SEG-007 | CRITICA | Edicion directa de ventas historicas | `Views\VentasCrudWindow.xaml.cs`, `ActualizarVenta_Click` | Alteracion de totales | Bloquear edicion directa; usar ajustes autorizados. |
| SEG-008 | ALTA | Borrado fisico de productos/clientes/usuarios | `InventarioWindow.xaml.cs`, `ClientePage.xaml.cs`, `VentanaUsuarios.xaml.cs` | Perdida de integridad historica | Soft delete/estado y auditoria. |
| SEG-009 | ALTA | Errores tecnicos mostrados al usuario | Multiples `MessageBox.Show(... ex.Message ...)` | Exposicion de detalles internos | Logging seguro y mensajes controlados. |
| SEG-010 | CRITICA | Stock puede quedar negativo por concurrencia | `VentasPage.xaml.cs`, `ActualizarStockProducto`; `DonacionesPage.xaml.cs`, update stock | Perdida financiera/inventario incorrecto | UPDATE condicionado por stock en API. |
| SEG-011 | CRITICA | Saldo cliente puede quedar negativo por concurrencia | `VentasPage.xaml.cs`, `DescontarSaldoCliente` | Perdida financiera | UPDATE condicionado por saldo suficiente en API. |
| SEG-012 | ALTA | Ingreso/retiro/cierre usan fecha/hora del cliente | `IngresoCajaPage.xaml.cs`, `RetirosCajaPage.xaml.cs`, `CierreCajaPage.xaml.cs` | Manipulacion temporal o descuadres | Fecha/hora de servidor. |
| SEG-013 | CRITICA | Retiro de caja no valida disponible transaccionalmente | `RetirosCajaPage.xaml.cs`, `RegistrarRetiro_Click`; `CajaHelper.ObtenerDineroAcumuladoCajaChica` | Retiro superior al efectivo real | Validacion server-side en transaccion. |
| SEG-014 | CRITICA | Cierre sin turno, esperado/contado/diferencia formal | `cierre_caja` y `CierreCajaPage.xaml.cs` | Cierre no auditable | Modelo CajaTurno/MovimientoCaja/CierreCaja. |
| SEG-015 | ALTA | Tipo de cambio sin auditoria | `TipoCambioHelper.cs`, `GuardarTipoCambio`; tabla `TipoCambioDolar` | Cambios no trazables | Auditoria y guardar tasa usada por venta. |

## 3. Credenciales expuestas

Archivo:

- `PulperiaPOS\DataAccess\DBConnection.cs`

Evidencia:

- Campo `connectionString` con servidor, base de datos, usuario y password.

Riesgo:

- La aplicacion WPF distribuida puede revelar credenciales.
- Cualquier usuario con el binario o codigo podria intentar conectarse directo a Azure SQL.

Orden futuro:

1. Rotar credencial expuesta.
2. Crear secreto por ambiente para API/backend.
3. Reducir permisos de la cuenta SQL.
4. Retirar `DBConnection` del cliente WPF cuando exista API.

## 4. Autenticacion actual

Flujo:

- `LoginWindow.ObtenerDatosUsuario` calcula `Seguridad.HashContrasena`.
- Consulta `usuario` por `nombre` y `contrasena`.

Debilidades:

- SHA-256 sin sal.
- No hay politica de intentos fallidos.
- No hay expiracion de sesion.
- No hay token ni claims.
- El rol se guarda en `UserSession.RolUsuario` dentro del cliente.

Migracion recomendada:

- Login API.
- BCrypt/Argon2 para nuevos hashes.
- Compatibilidad temporal: si login SHA-256 legado es exitoso, rehash a BCrypt/Argon2.
- Permisos server-side por accion.

## 5. Autorizacion actual

La autorizacion depende de:

- Abrir `VentanaAdministrador` o `VentanaAnfitrion`.
- Ocultar botones en algunas pantallas cuando `UserSession.RolUsuario == "Anfitrion"`.

Riesgo:

- La UI no es una barrera suficiente.
- Un cliente modificado podria ejecutar SQL directo si conserva credenciales.

Permisos que deben existir en API:

- `ventas.crear`
- `ventas.anular`
- `ventas.devolver`
- `inventario.ver`
- `inventario.modificar`
- `clientes.ver`
- `clientes.modificar`
- `clientes.saldo.modificar`
- `caja.ingresar`
- `caja.retirar`
- `caja.cerrar`
- `caja.reabrir`
- `usuarios.administrar`
- `reportes.ver`
- `tipoCambio.modificar`

## 6. Excepciones SQL visibles

Patron encontrado:

- Multiples capturas hacen `MessageBox.Show("Error ... " + ex.Message)`.
- En ventas se muestra incluso `ex.StackTrace` al buscar producto.

Riesgo:

- Revela estructura interna, nombres de tablas, errores SQL y potencialmente detalles de conexion.

Futuro:

- Logging centralizado en API.
- Correlation ID.
- Mensajes de usuario sin detalles tecnicos.

## 7. Queries concatenadas

No se observaron concatenaciones directas de entrada del usuario en SQL para los flujos principales; se usan parametros en la mayoria de consultas revisadas.

Observacion:

- `CajaHelper.ObtenerTotalesCaja` arma parte de la consulta con interpolacion (`filtroFecha`, `filtroHora`) pero los valores son constantes internas, no entrada de usuario.
- Aun asi, el patron deberia reemplazarse por consultas server-side claras.

## 8. Acciones no permitidas para Fase 0

No se corrigio ningun hallazgo en esta fase. Este documento solo registra el estado actual.
