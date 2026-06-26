# Control de acceso - Productos de ventas por API

Fecha UTC: 2026-06-26 03:16:42 UTC

## Superficie protegida

La lectura de productos para `VentasPage` usa:

- `GET /api/productos`
- `GET /api/productos/{idProducto}`

## Permiso requerido

```text
Inventario.Ver
```

## Resultado esperado

- Token valido con `Inventario.Ver`: HTTP 200.
- Token ausente: HTTP 401.
- Token valido sin `Inventario.Ver`: HTTP 403.
- Producto inexistente por id: HTTP 404 seguro.
- Parametros invalidos: HTTP 400 seguro.

## Campos permitidos

- `idProducto`
- `nombre`
- `precio`
- `stockDisponible`
- `disponible`

## Campos no expuestos

- `costo`
- `proveedor`
- `vendido`
- margenes
- historial
- datos administrativos innecesarios

## Validacion ejecutada

- Acceso con permiso `Inventario.Ver`: confirmado HTTP 200.
- Acceso sin token: confirmado HTTP 401.
- Acceso sin permiso requerido: confirmado HTTP 403.
- Producto inexistente: confirmado HTTP 404.
- Limit invalido: confirmado HTTP 400.

No se imprimieron ni documentaron tokens, usuarios, passwords, hashes, signing keys, connection strings, productos reales, codigos, precios ni existencias.

## Riesgos pendientes

- La API de lectura aun no es autoridad final de stock o precio al pagar.
- La venta sigue siendo transaccional en WPF con SQL directo.
- La validacion visual con usuario real debe confirmar permisos y experiencia de busqueda.

## Recomendacion

Mantener `UseVentasProductosApi=false` por defecto hasta completar la validacion manual con operador.
