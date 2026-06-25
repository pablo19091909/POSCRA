# Matriz propuesta de roles y permisos

## Roles base

| Rol | Descripcion |
| --- | --- |
| `Administrador` | Gestion completa del POS y operaciones sensibles. |
| `Anfitrion` | Operacion diaria con permisos limitados. |

## Principio de aplicacion

La WPF podra ocultar o mostrar controles para mejorar UX, pero la autorizacion real debe ocurrir en `POS.Api` para cada endpoint y accion critica.

## Permisos propuestos

| Permiso | Operacion protegida | Roles sugeridos | Riesgo sin validacion server-side | Requiere |
| --- | --- | --- | --- | --- |
| `Usuarios.Administrar` | Crear, editar, desactivar/reactivar usuarios y asignar roles. | Administrador | Escalamiento de privilegios, usuarios no auditables. | API + DB + WPF |
| `Usuarios.Ver` | Listar y consultar usuarios sin hashes. | Administrador | Exposicion innecesaria de cuentas. | API + DB + WPF |
| `Ventas.Crear` | Registrar venta y detalle, descontar inventario, actualizar saldo. | Administrador, Anfitrion | Ventas falsas, stock inconsistente, manipulacion de saldo. | API + DB + WPF |
| `Ventas.Ver` | Consultar ventas y detalle. | Administrador, Anfitrion | Exposicion de ventas sin control. | API + DB + WPF |
| `Ventas.Anular` | Anular venta y reversar efectos. | Administrador | Perdida de ingresos o reversos no autorizados. | API + DB + WPF |
| `Ventas.Devolver` | Registrar devolucion parcial/total. | Administrador | Reembolsos indebidos y stock incorrecto. | API + DB + WPF |
| `Inventario.Ver` | Consultar productos, stock y reportes de inventario. | Administrador, Anfitrion | Exposicion de inventario. | API + DB + WPF |
| `Inventario.Editar` | Crear, editar, eliminar/desactivar productos y ajustar stock. | Administrador | Manipulacion de costos, precios o existencias. | API + DB + WPF |
| `Clientes.Ver` | Consultar clientes y saldos. | Administrador, Anfitrion | Exposicion de informacion de clientes. | API + DB + WPF |
| `Clientes.Editar` | Crear, editar o desactivar clientes. | Administrador | Cambios indebidos de clientes o comprobantes. | API + DB + WPF |
| `Clientes.AjustarSaldo` | Cargar, consumir, liberar o ajustar saldo. | Administrador | Perdida financiera y saldos no auditables. | API + DB + WPF |
| `Caja.Abrir` | Abrir gaveta o iniciar turno/caja futura. | Administrador | Apertura fisica no autorizada. | API + WPF |
| `Caja.Ingresar` | Registrar ingreso manual de caja. | Administrador | Incrementos falsos de caja. | API + DB + WPF |
| `Caja.Retirar` | Registrar retiro manual de caja. | Administrador | Retiro no autorizado de efectivo. | API + DB + WPF |
| `Caja.Cerrar` | Registrar cierre de caja. | Administrador, Anfitrion | Cierres incompletos o manipulados. | API + DB + WPF |
| `Caja.Reabrir` | Reabrir/anular cierre. | Administrador | Alteracion de periodos cerrados. | API + DB + WPF |
| `Caja.VerResumen` | Ver totales, ingresos, retiros y cierres. | Administrador, Anfitrion | Exposicion de informacion financiera. | API + DB + WPF |
| `Reportes.Ver` | Generar reportes de ventas, inventario, clientes o caja. | Administrador | Exposicion o extraccion masiva de datos. | API + DB + WPF |
| `TipoCambio.Ver` | Consultar tipo de cambio. | Administrador, Anfitrion | Bajo, pero debe ser consistente. | API + DB + WPF |
| `TipoCambio.Editar` | Crear o actualizar tipo de cambio. | Administrador | Manipulacion de conversiones y ventas. | API + DB + WPF |
| `Donaciones.Ver` | Consultar donaciones registradas. | Administrador | Exposicion de movimientos. | API + DB + WPF |
| `Donaciones.Registrar` | Registrar donacion y descontar inventario. | Administrador | Salida de inventario sin autorizacion. | API + DB + WPF |
| `Configuracion.Administrar` | Configuracion operativa futura del sistema. | Administrador | Cambio de reglas criticas sin control. | API + DB + WPF |

## Acciones actuales sin proteccion suficiente

| Accion | Estado actual | Permiso futuro |
| --- | --- | --- |
| Gestion de usuarios | Solo accesible desde ventana Admin, sin API. | `Usuarios.Administrar` |
| Edicion de inventario | Ocultamiento parcial para Anfitrion. | `Inventario.Editar` |
| Eliminacion de inventario | Boton UI, SQL directo. | `Inventario.Editar` |
| Edicion/eliminacion de clientes | Ocultamiento parcial para Anfitrion. | `Clientes.Editar` |
| Liberacion de saldo | Disponible desde clientes; requiere control fuerte. | `Clientes.AjustarSaldo` |
| Anulacion/eliminacion de ventas | SQL directo y borrado fisico. | `Ventas.Anular` |
| Ingreso/retiro de caja | Disponible desde Admin. | `Caja.Ingresar`, `Caja.Retirar` |
| Cierre de caja | Disponible a Admin y Anfitrion. | `Caja.Cerrar` |
| Tipo de cambio | Disponible a Admin y Anfitrion. | `TipoCambio.Ver`, `TipoCambio.Editar` |
| Donaciones | Disponible a Admin. | `Donaciones.Registrar` |

## Reglas recomendadas

- Cada endpoint debe declarar permiso requerido.
- El JWT debe incluir permisos o un identificador para resolverlos server-side.
- El servidor debe rechazar con 403 si el usuario no tiene permiso.
- WPF no debe decidir permisos finales.
- Los permisos deben versionarse y probarse con casos positivos y negativos.
