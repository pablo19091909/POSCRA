# Fase 3A - Analisis de usuarios y autenticacion

## Alcance

Esta fase analiza el modelo actual de usuarios, roles, contrasenas y permisos para preparar una implementacion futura en `POS.Api`.

No se implemento login API, JWT, cambios de base de datos, cambios WPF ni cambios de contrasenas.

## Fuentes revisadas

### Base de datos y documentacion

- `docs/03_BaseDatos/TABLAS_Y_RELACIONES_ENCONTRADAS.md`
- `docs/00_InventarioTecnico/FLUJOS_CRITICOS_ACTUALES.md`
- `docs/00_InventarioTecnico/MATRIZ_MODULOS_Y_SQL_DIRECTO.md`
- `docs/01_SeguridadActual/HALLAZGOS_INICIALES_SEGURIDAD.md`

### Codigo WPF

- `PulperiaPOS/LoginWindow.xaml.cs`
- `PulperiaPOS/Seguridad.cs`
- `PulperiaPOS/UserSession.cs`
- `PulperiaPOS/VentanaUsuarios.xaml.cs`
- `PulperiaPOS/VentanaEditarUsuario.xaml`
- `PulperiaPOS/VentanaEditarUsuario.xaml.cs`
- `PulperiaPOS/VentanaAdministrador.xaml`
- `PulperiaPOS/VentanaAdministrador.xaml.cs`
- `PulperiaPOS/VentanaAnfitrion.xaml`
- `PulperiaPOS/VentanaAnfitrion.xaml.cs`
- `PulperiaPOS/ClientePage.xaml.cs`
- `PulperiaPOS/InventarioWindow.xaml.cs`
- `PulperiaPOS/VentasPage.xaml.cs`
- `PulperiaPOS/Views/VentasCrudWindow.xaml.cs`
- `PulperiaPOS/IngresoCajaPage.xaml.cs`
- `PulperiaPOS/Views/RetirosCajaPage.xaml.cs`
- `PulperiaPOS/Views/CierreCajaPage.xaml.cs`
- `PulperiaPOS/TipoCambioWindow.xaml.cs`
- `PulperiaPOS/DonacionesPage.xaml.cs`

## Modelo actual de usuarios

La tabla `usuario` esta documentada con estos campos:

| Campo | Uso observado |
| --- | --- |
| `idUsuario` | Llave primaria identity. Se usa en login y en ventas mediante `ventas.usuario_id`. |
| `nombre` | Nombre de usuario/login. Se consulta como identificador de acceso. |
| `contrasena` | Hash de contrasena legado. No se documentan valores. |
| `rol` | Texto libre/nullable usado por WPF para decidir ventana inicial. |

No se observaron campos de:

- estado activo/inactivo;
- bloqueo por intentos fallidos;
- fecha de ultimo login;
- fecha de creacion/modificacion;
- usuario creador/modificador;
- version de hash;
- expiracion o cambio obligatorio de contrasena.

## Relaciones con movimientos

| Area | Relacion actual |
| --- | --- |
| Ventas | `ventas.usuario_id` referencia `usuario.idUsuario`. |
| Ingresos de caja | Guarda usuario como texto, no FK fuerte a `usuario`. |
| Retiros de caja | No guarda usuario responsable en base de datos. |
| Cierres de caja | No guarda usuario responsable ni turno. |
| Saldo liberado | No guarda usuario responsable. |
| Tipo de cambio | No guarda usuario responsable ni historial. |

## Login actual

Flujo observado:

1. `LoginWindow` toma usuario y contrasena desde la UI.
2. `Seguridad.HashContrasena` calcula SHA-256 simple.
3. WPF consulta directamente `usuario` por `nombre` y `contrasena`.
4. Si encuentra registro, guarda `idUsuario`, `nombre` y `rol` en `UserSession`.
5. Si el rol es `Administrador`, abre `VentanaAdministrador`.
6. Si el rol es `Anfitrion`, abre `VentanaAnfitrion`.

Riesgos:

- autenticacion ocurre en cliente WPF;
- el cliente conoce el algoritmo de hash;
- no hay rate limiting;
- no hay bloqueo por intentos fallidos;
- errores tecnicos pueden mostrarse al usuario;
- no se valida estado activo/inactivo porque no existe campo;
- WPF decide permisos por rol local.

## Metodo actual de contrasenas

El metodo actual usa:

- SHA-256;
- entrada en UTF-8;
- salida hexadecimal en mayusculas;
- sin sal;
- sin factor de costo;
- sin algoritmo adaptativo.

No se deben documentar ni exponer hashes reales.

## Administracion actual de usuarios

`VentanaUsuarios` carga `SELECT * FROM usuario` y permite:

- listar usuarios;
- buscar por nombre o rol;
- agregar usuarios;
- actualizar nombre, contrasena y rol;
- eliminar usuarios fisicamente.

`VentanaEditarUsuario` valida duplicados por nombre antes de guardar. La validacion ocurre en cliente y no reemplaza un indice unico en base de datos.

Riesgos:

- el hash queda cargado en memoria/UI como propiedad `contrasena`;
- al editar un usuario, el campo password recibe el valor existente y luego se vuelve a hashear si se guarda;
- eliminacion fisica puede romper trazabilidad historica;
- no hay estado activo/inactivo;
- no hay auditoria;
- no hay autorizacion server-side.

## Roles actuales

Roles observados en codigo y UI:

- `Administrador`
- `Anfitrion`

El `ComboBox` de creacion/edicion solo ofrece esos dos roles. El login solo abre ventanas para esos dos valores.

## Uso actual de roles

| Rol | Acceso observado |
| --- | --- |
| `Administrador` | Inventario, clientes, ventas, donaciones, cierre de caja, ingreso de caja, retiro de caja, usuarios, tipo de cambio, abrir caja, prueba de impresion. |
| `Anfitrion` | Inventario, clientes, ventas, cierre de caja, tipo de cambio. |

Restricciones parciales:

- En `ClientePage`, `Anfitrion` no ve botones de agregar, editar y eliminar clientes.
- En `InventarioWindow`, `Anfitrion` no ve boton de agregar ni columna de acciones.

No se observo autorizacion server-side. Las protecciones son principalmente navegacion por ventana y visibilidad de botones.

## Brechas de seguridad

| Brecha | Riesgo |
| --- | --- |
| Login en WPF contra SQL directo | El cliente decide autenticacion y puede ser manipulado. |
| SHA-256 simple sin sal | Vulnerable si se filtran hashes. |
| Sin estado activo/inactivo | Un usuario no puede desactivarse sin eliminarlo. |
| Sin rate limiting | Mayor riesgo de fuerza bruta. |
| Sin bloqueo por intentos fallidos | Ataques repetidos sin control central. |
| Roles como texto nullable | Valores invalidos o nulos pueden romper reglas. |
| Permisos por UI | Ocultar botones no protege operaciones reales. |
| Eliminacion fisica de usuarios | Perdida de trazabilidad historica. |
| Caja sin usuario/FK en varias tablas | Auditoria debil. |
| Tipo de cambio sin auditoria | Cambios financieros sin responsable. |
| No hay permisos granulares | Administrador/Anfitrion no cubren acciones criticas con precision. |

## Recomendacion para Fase 3B

Implementar en `POS.Api` la base de autenticacion sin migrar modulos funcionales:

1. Servicio de verificacion de contrasena compatible con SHA-256 legado y hash moderno.
2. Endpoint `POST /api/auth/login`.
3. Emision de JWT con claims minimos.
4. Politicas de autorizacion por permisos.
5. Validacion de usuario activo cuando exista campo de estado.
6. Rate limiting para login.
7. Sin modificar todavia ventas, inventario, caja, clientes ni reportes.
