# Fase 3H - Productos de ventas por API solo lectura

Fecha UTC: 2026-06-26 03:16:42 UTC

## Objetivo

Migrar de forma gradual y reversible la carga, busqueda, consulta de precio y disponibilidad de productos dentro de `VentasPage` hacia `POS.Api`, manteniendo la creacion de ventas por SQL directo.

## Modelo real encontrado

Tabla principal: `inventario`.

Columnas encontradas:

- `idProducto`: `nvarchar`, no nulo. Se usa como llave primaria operativa y codigo de producto.
- `nombre`: `nvarchar`, no nulo. Se usa para busqueda y descripcion visual.
- `proveedor`: `nvarchar`, nullable. Administrativo, no expuesto por API.
- `costo`: `decimal`, nullable. Administrativo, no expuesto por API.
- `precio`: `decimal`, nullable. Usado por ventas.
- `stock`: `int`, nullable. Usado por ventas como disponibilidad.
- `vendido`: `int`, nullable. Administrativo/estadistico, no expuesto por API.

Campos usados por `VentasPage`: `idProducto`, `nombre`, `precio`, `stock`.

Campos usados por `InventarioWindow`: `idProducto`, `nombre`, `proveedor`, `costo`, `precio`, `stock`, `vendido`.

La regla actual de ventas considera no disponible un producto con `stock <= 0`. Al agregar al carrito se copia `idProducto`, `nombre`, `precio` y `stock` al modelo local `ProductoVenta`.

## Endpoints creados

- `GET /api/productos`
- `GET /api/productos/{idProducto}`

Parametros admitidos por `GET /api/productos`:

- `busqueda`
- `codigo`
- `soloDisponibles`
- `limit`
- `offset`

## Permiso requerido

Los endpoints requieren JWT valido y permiso `Inventario.Ver`.

## Campos expuestos

- `idProducto`
- `nombre`
- `precio`
- `stockDisponible`
- `disponible`

No se exponen costo, proveedor, vendido, margenes, historial ni datos administrativos.

## Implementacion

Se crearon contratos, servicio, repositorio y controller para productos. El repositorio usa `Microsoft.Data.SqlClient`, `SqlConnectionFactory`, consultas parametrizadas y `CancellationToken`.

En WPF se creo `ProductosApiClient`, modelo de respuesta y el flag `UseVentasProductosApi`.

## Comportamiento por flag

Con `UseVentasProductosApi=false`, `VentasPage` mantiene el flujo SQL directo original para busqueda y agregado de productos.

Con `UseVentasProductosApi=true`, `VentasPage` usa `ProductosApiClient` para:

- buscar producto por texto o codigo;
- cargar sugerencias de busqueda;
- leer precio y stock informativo;
- agregar al carrito con los mismos datos requeridos por el flujo actual.

Si la API falla con el flag activo, se muestra un mensaje seguro y no se consulta SQL como fallback automatico.

## Limitacion importante

La API de esta fase es solo lectura. El precio y stock devueltos son informativos para WPF. La futura API de ventas debera volver a validar precio, stock y reglas transaccionales al momento de pagar.

## Pruebas realizadas

- Token valido con `Inventario.Ver`: HTTP 200.
- Sin token: HTTP 401.
- Token sin `Inventario.Ver`: HTTP 403.
- Busqueda vacia: HTTP 200.
- Busqueda por nombre: HTTP 200.
- Busqueda por codigo: respuesta valida.
- Busqueda con caracteres especiales: HTTP 200.
- Producto inexistente: HTTP 404.
- Limit invalido: HTTP 400.
- `/health`: HTTP 200.
- `/health/database`: HTTP 200.
- `/api/system/version`: HTTP 200.

## Comparacion agregada

- SQL: 220 productos.
- API: 220 productos.
- SQL disponibles: 115.
- API disponibles: 115.
- SQL no disponibles: 105.
- API no disponibles: 105.

No se listaron productos, codigos, precios ni existencias reales.

## Confirmaciones

- No se modifico base de datos.
- No se ejecutaron scripts SQL ni migraciones.
- No se modificaron precios.
- No se modifico stock.
- No se modifico creacion de ventas.
- No se modificaron pagos, caja, cierres, reportes, comprobantes, saldo de clientes ni tipo de cambio.
- `Authentication:EnableLegacyHashUpgrade` permanece en `false`.

## Plan de reversion

Establecer `FeatureFlags:UseVentasProductosApi=false` en la configuracion local del WPF o eliminar el override local. El valor versionado queda en `false`.

## Pendientes

- Validacion visual manual completa en WPF con `UseVentasProductosApi=true`.
- Migrar la creacion de ventas a API transaccional en una fase posterior.
- Validar precio y stock nuevamente en la futura API de ventas.

## Recomendacion

Siguiente fase recomendada: ejecutar una validacion manual operativa de `VentasPage` con `UseVentasProductosApi=true`, sin confirmar ventas, y despues disenar la API transaccional de creacion de ventas.
