# Fase 3H.1 - Validacion visual y operativa de productos en VentasPage por API

Fecha UTC: 2026-06-26 10:36:30 UTC

## Alcance

Validacion de productos de `VentasPage` usando `POS.Api` en modo solo lectura. No se modifico codigo, no se modifico base de datos y no se confirmaron ventas.

## Flags usados

Valores versionados:

- `UseApiLogin=false`
- `UseVentasClienteSelectorApi=false`
- `UseVentasProductosApi=false`

Valores locales detectados durante esta validacion:

- `UseApiLogin`: no configurado localmente.
- `UseVentasClienteSelectorApi`: no configurado localmente.
- `UseVentasProductosApi`: no configurado localmente.

Valor final documentado: `UseVentasProductosApi` permanece desactivado por defecto. No se modifico configuracion local en esta fase.

`Authentication:EnableLegacyHashUpgrade=false`.

## API disponible

- `/health`: HTTP 200.
- `/health/database`: HTTP 200.
- `/api/system/version`: HTTP 200.
- API disponible por HTTPS local.

## Pruebas API de productos

- Token valido con `Inventario.Ver`: HTTP 200.
- Sin token: HTTP 401.
- Token valido sin `Inventario.Ver`: HTTP 403.
- Busqueda vacia: HTTP 200.
- Busqueda por nombre: HTTP 200.
- Busqueda por codigo: respuesta valida.
- Busqueda con caracteres especiales: HTTP 200.
- Producto inexistente: HTTP 404.
- Limit invalido: HTTP 400.

No se imprimieron ni documentaron productos, codigos, precios, existencias, usuarios, tokens ni secretos.

## Validacion visual WPF

La prueba visual operada por usuario no fue ejecutada dentro de esta sesion.

Pendiente de confirmar manualmente con `UseApiLogin=true`, `UseVentasClienteSelectorApi=true` y `UseVentasProductosApi=true` en configuracion local:

- productos cargan correctamente en `VentasPage`;
- busqueda por nombre funciona;
- busqueda por codigo funciona;
- busqueda inexistente muestra estado controlado;
- caracteres especiales no generan error visual;
- producto disponible muestra precio y stock coherentes;
- producto disponible puede agregarse al carrito;
- producto no disponible se maneja igual que el flujo anterior;
- no aparecen errores SQL ni detalles tecnicos;
- no se presiona `Pagar`;
- logout limpia la sesion.

## Ruta API sin fallback SQL

Validado por codigo existente de Fase 3H:

- Con `UseVentasProductosApi=true`, la busqueda y sugerencias de productos entran por `ProductosApiClient`.
- Las ramas API no llaman `DBConnection.GetConnection()` para esas operaciones.
- Si API falla, se muestra mensaje seguro y no se ejecuta fallback automatico a SQL.
- Con `UseVentasProductosApi=false`, la ruta SQL previa permanece disponible.

## Pruebas de error

Validado automaticamente:

- Sin token: HTTP 401.
- Sin permiso `Inventario.Ver`: HTTP 403.
- Producto inexistente: HTTP 404.
- Parametros invalidos: HTTP 400.

Pendiente de prueba visual manual:

- API apagada con flag activo muestra mensaje seguro sin fallback SQL.
- Token expirado limpia sesion y vuelve al login de forma controlada.
- Usuario real sin `Inventario.Ver` recibe acceso denegado seguro.

## Comparacion agregada SQL versus API

- Total productos SQL: 220.
- Total productos API: 220.
- Productos disponibles SQL: 115.
- Productos disponibles API: 115.
- Productos no disponibles SQL: 105.
- Productos no disponibles API: 105.
- Stock agregado actual SQL: registrado solo como control agregado interno, sin listar productos.

## Datos sensibles y datos de negocio

Consultas agregadas de control ejecutadas solo con `SELECT`.

Estado agregado observado al cierre:

- Ventas totales existentes: 1881.
- Detalles de venta existentes: 4921.
- Ingresos de caja existentes: 9.
- Retiros de caja existentes: 6.
- Cierres de caja existentes: 15.

Durante esta validacion no se ejecutaron escrituras, no se confirmo ninguna venta y no se realizaron movimientos de inventario, saldo ni caja desde Codex.

## Compilacion

- WPF: compila con 0 errores.
- POS.Api: compila con 0 errores.

## Riesgos pendientes

- Falta validacion visual real con operador y flags locales activos.
- La API de productos sigue siendo solo lectura; la futura API de ventas debe validar precio y stock al pagar.
- Mientras la venta siga guardandose por SQL directo, no debe presionarse `Pagar` durante pruebas de lectura.

## Recomendacion

Siguiente fase recomendada: ejecutar la prueba manual de `VentasPage` con los tres flags locales activos, sin confirmar ventas, y luego avanzar al diseno de la API transaccional de creacion de ventas.
