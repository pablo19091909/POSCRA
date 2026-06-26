# Fase 3G - Selector de cliente en ventas mediante API

Fecha UTC: 2026-06-26 02:58:37 UTC

## Objetivo

Migrar de forma gradual y reversible la carga, busqueda y seleccion de clientes dentro de `VentasPage` para que pueda usar `POS.Api`, sin modificar la logica de venta ni datos de negocio.

## Flujo encontrado

El selector de cliente de ventas cargaba directamente desde SQL con `SELECT idCliente, nombre FROM cliente`, mantenia una lista local para filtrar por texto y consultaba el saldo del cliente seleccionado desde SQL cuando era necesario para validaciones visuales y de pago.

## Implementacion

Se agrego el feature flag `FeatureFlags:UseVentasClienteSelectorApi`, con valor `false` por defecto. Cuando el flag esta en `false`, `VentasPage` conserva el flujo SQL existente. Cuando el flag esta en `true`, la carga inicial y la busqueda del selector usan `ClientesApiClient` contra `GET /api/clientes`.

No se creo un endpoint nuevo porque el endpoint existente de clientes cubre los campos requeridos: `idCliente`, `nombre`, `saldo` y `comprobante`.

## Alcance en VentasPage

Cambios realizados solo en:

- Carga inicial del selector de cliente.
- Busqueda del selector de cliente.
- Uso del saldo ya recibido desde API para el cliente seleccionado.
- Manejo seguro de errores cuando el selector usa API.

No se modifico:

- Confirmacion de venta.
- Insercion de venta.
- Insercion de detalle de venta.
- Actualizacion de inventario.
- Actualizacion de saldo.
- Metodos de pago.
- Caja.
- Cierres.
- Reportes.
- Productos.

## Comportamiento por flag

Con `UseVentasClienteSelectorApi=false`, el selector usa SQL directo como antes.

Con `UseVentasClienteSelectorApi=true`, el selector usa API para cargar y buscar clientes. Si la API falla, se muestra un error seguro y no hay fallback silencioso a SQL.

## Validaciones realizadas

- Compilacion completa de la solucion: exitosa, 0 errores.
- WPF: compila correctamente.
- POS.Api: compila correctamente.
- `/health`: HTTP 200.
- `/health/database`: HTTP 200.
- `/api/system/version`: HTTP 200.
- `/api/clientes` con permiso `Clientes.Ver`: HTTP 200.
- `/api/clientes` sin token: HTTP 401.
- `/api/clientes` sin permiso requerido: HTTP 403.
- Busqueda vacia: HTTP 200.
- Busqueda por texto: HTTP 200.
- Busqueda con caracteres especiales: HTTP 200.
- Busqueda sin resultados: HTTP 200 con lista vacia.

## Comparacion agregada

- Total agregado SQL: 162 clientes.
- Total agregado API: 162 clientes.
- Clientes con saldo distinto de cero por SQL: 109.
- Clientes con saldo distinto de cero por API: 109.

No se listaron clientes ni datos personales.

## Seguridad

El selector requiere autenticacion y permiso `Clientes.Ver` cuando usa API. No se imprimieron tokens, credenciales, cadenas de conexion, usuarios, hashes ni datos sensibles.

`Authentication:EnableLegacyHashUpgrade` permanece en `false`.

## Riesgos pendientes

- La prueba visual completa con operador debe confirmar el comportamiento exacto del ComboBox con `UseVentasClienteSelectorApi=true`.
- La venta sigue usando SQL directo para escritura y operaciones transaccionales, por diseno de esta fase.
- Mientras exista el rollback SQL, debe mantenerse la proteccion de credenciales locales.

## Recomendacion

Siguiente fase recomendada: ejecutar una validacion manual operativa del selector de cliente en `VentasPage` con `UseVentasClienteSelectorApi=true`, sin registrar ventas, y luego migrar de forma controlada la consulta de productos de ventas a API solo lectura.
