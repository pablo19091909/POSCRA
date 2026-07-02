# Validacion no dual write ventas WPF

## Ruta habilitada

Con `UseVentasApiEfectivoWrite=true`, el metodo `Efectivo` en `VentasPage` usa la ruta API y no entra al flujo historico `PagarConSql`.

La ruta efectiva API:

- valida turno abierto por `CajaApiClient`;
- obtiene pre-cierre por API;
- crea la solicitud con `VentasApiClient`;
- conserva la idempotencia de la intencion;
- limpia la intencion solo con respuesta exitosa.

## Ausencia de fallback

En la ruta efectiva API:

- no se ejecuta `DBConnection` para persistir venta;
- no se ejecuta `CajaHelper`;
- no se ejecuta `RawPrinterHelper`;
- no se descuenta inventario localmente;
- no se abre gaveta por flujo historico;
- no se imprime recibo historico.

## Doble clic

Hallazgo durante la prueba:

- el servidor no duplico la venta;
- la UI mostro doble confirmacion/mensaje al doble clic.

Correccion aplicada:

- `ventaApiEnProceso=true` se establece al inicio de `PagarConApiAsync`;
- `PagarButton` se deshabilita antes de validar turno y antes de mostrar confirmacion;
- los controles se bloquean durante el envio.

## Validacion agregada

Despues de la venta WPF:

- solo una venta nueva;
- solo un pago nuevo;
- solo una idempotencia de venta nueva;
- solo un movimiento `VentaEfectivo` nuevo;
- sin descuento duplicado;
- sin ingresos, retiros, ajustes o reversas.
