# Fase 4E.1 - Integracion WPF Ventas API bloqueada

Fecha UTC: 2026-06-26

## Alcance

Se preparo `VentasPage` para enviar ventas a `POST /api/ventas` solamente cuando el feature flag WPF `FeatureFlags:UseVentasApiWrite` este activo. El valor versionado queda en `false`.

La ruta SQL historica permanece disponible cuando `UseVentasApiWrite=false`. No se elimino el flujo SQL existente y no se modifico `DBConnection.cs`.

## Componentes

- `PulperiaPOS/Configuration/FeatureFlags.cs`: agrega `UseVentasApiWrite`.
- `PulperiaPOS/appsettings.json`: agrega `UseVentasApiWrite=false`.
- `PulperiaPOS/appsettings.Development.json.example`: agrega `UseVentasApiWrite=false`.
- `PulperiaPOS/ApiClients/VentasApiClient.cs`: cliente autenticado para `POST /api/ventas`.
- `PulperiaPOS/Models/Ventas/*`: contratos WPF y coordinador de idempotencia en memoria.
- `PulperiaPOS/VentasPage.xaml`: indicador discreto `API Test`.
- `PulperiaPOS/VentasPage.xaml.cs`: separacion de rutas SQL/API por feature flag.
- `PulperiaPOS/ApiClients/ApiClientBase.cs`: reconocimiento seguro de `409 Conflict`.
- `PulperiaPOS/Models/Api/ApiErrorType.cs`: agrega `Conflict`.

## Ruta SQL

Con `UseVentasApiWrite=false`, `Pagar_Click` llama `PagarConSql()`. El cuerpo SQL conserva las validaciones, insercion de venta, detalle, stock, saldo, impresion, apertura de caja y limpieza que ya existian.

## Ruta API

Con `UseVentasApiWrite=true`, `Pagar_Click` llama `PagarConApiAsync()` y no llama metodos SQL de insercion, stock ni saldo.

La solicitud enviada contiene intencion de venta:

- `clienteId`
- items con `productoId` y `cantidad`
- un pago
- metodo de pago
- monto recibido solo cuando aplica
- voucher o referencia cuando aplica
- `idempotencyKey`

WPF no envia como autoridad precio, subtotal, total final, stock, saldo, vuelto definitivo, usuario, factura, fecha oficial ni estado financiero.

## Idempotencia

`VentaSubmissionCoordinator` mantiene en memoria una intencion pendiente. Si el intento falla por timeout, red, 503, 409 u otro error seguro, el carrito no se limpia y se conserva la misma key mientras la intencion sea la misma. Si la intencion cambia, se genera una nueva key.

La key no se guarda en archivos, logs, configuracion ni base local.

## Validaciones UI

Implementado en la ruta API:

- requiere cliente real con id valido;
- requiere carrito con cantidades positivas;
- permite `Efectivo`, `Tarjeta`, `Sinpe`, `Saldo Cliente`;
- bloquea `Dolares` en modo API;
- rechaza metodos no soportados;
- requiere monto recibido en efectivo;
- requiere voucher para tarjeta;
- requiere referencia para Sinpe;
- no implementa pagos combinados.

## Errores y no fallback

Si la API falla, no se registra por SQL. El carrito no se limpia automaticamente. Los mensajes son seguros y no exponen host, SQL, token, stack trace, idempotency key ni detalles internos.

## Impresion

En modo API, el recibo se imprime solo despues de respuesta exitosa. Usa factura, total, vuelto, metodo, fecha y detalles devueltos por API. No imprime ante timeout, red, 409, 503 u otros errores.

## Pruebas no destructivas

- WPF: compilacion correcta, 0 errores.
- API: compilacion correcta, 0 errores.
- Solucion completa: compilacion correcta, 0 errores.
- `/health`: HTTP 200.
- `/health/database`: HTTP 200.
- `/api/system/version`: HTTP 200.
- `POST /api/ventas` sin token: HTTP 401, bloqueado.
- No se activo `UseVentasApiWrite` en configuracion versionada.
- No se activo `EnableVentasApiWrite` en configuracion versionada API.
- No se ejecuto venta real.

## Integridad agregada

Lecturas SELECT agregadas antes y despues:

| Tabla | Antes | Despues |
| --- | ---: | ---: |
| ventas | 1889 | 1889 |
| DetalleVenta | 4933 | 4933 |
| inventario | 225 | 225 |
| cliente | 167 | 167 |
| venta_idempotencia | 7 | 7 |
| venta_pago | 7 | 7 |
| venta_auditoria | 7 | 7 |
| ingreso_caja | 9 | 9 |
| retiro_caja | 6 | 6 |
| cierre_caja | 15 | 15 |

## Riesgos pendientes

- Falta prueba manual WPF en Test con ambos flags activos.
- Falta validar una venta API desde WPF con token real y permiso `Ventas.Crear`.
- Falta validar reintento real tras timeout/red.
- CajaTurno y MovimientoCaja siguen fuera de alcance.
- Dolares, Donacion y pagos combinados siguen fuera de Venta API V1.

## Recomendacion

Pasar a Fase 4E.2: activacion temporal controlada en Test con `UseVentasApiWrite=true`, `EnableVentasApiWrite=true`, usuario con `Ventas.Crear`, una venta sintetica por metodo soportado y verificacion agregada posterior.
