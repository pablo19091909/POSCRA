# Matriz de rutas historicas y reemplazo API

| Ruta | Estado actual | Clasificacion | Accion futura |
| --- | --- | --- | --- |
| `VentasPage` venta historica SQL | Disponible segun flags | Escritura historica a restringir | Reemplazar gradualmente por `POST /api/ventas` |
| `VentasPage` venta efectivo API | Validada | Ruta API piloto | Mantener con monitoreo |
| `VentasCrudWindow` historial | SQL directo | Lectura historica permitida | Migrar lectura a reporterÃ­a API |
| `VentasCrudWindow` borrar venta | SQL historico | Escritura historica a bloquear para ventas API | Bloquear para ventas con origen API |
| `VentasCrudWindow` reversa API | Validada | Ruta API obligatoria para ventas API | Mantener detras de permiso |
| `DetalleVentaWindow` | SQL directo | Lectura historica permitida | Migrar a detalle API/reportes |
| `IngresoCajaPage` historico | SQL directo | Escritura historica fuera de corte ventas | Mantener por fase de caja |
| `RetirosCajaPage` historico | SQL directo | Escritura historica fuera de corte ventas | Mantener por fase de caja |
| `CierreCajaPage` historico | SQL directo | Escritura historica fuera de corte ventas | Mantener separado de Caja API |
| `ClientePage` reporte saldos | SQL directo | Lectura historica permitida | Evaluar reporte API clientes |
| `InventarioWindow` | SQL directo | Lectura historica permitida | Evaluar inventario API |
| `RawPrinterHelper` recibos | Impresion historica | Fuera de reporte financiero | No usar como fuente de verdad |

## Reglas de corte

- No ocultar controles historicos sin piloto.
- No bloquear escritura historica general hasta completar reporterÃ­a.
- Bloquear primero solo borrado fisico sobre ventas API.
- Mantener etiqueta de origen en toda lectura combinada.


