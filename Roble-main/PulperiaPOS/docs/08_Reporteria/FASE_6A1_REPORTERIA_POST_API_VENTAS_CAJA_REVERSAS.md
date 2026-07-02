# Fase 6A.1 - Reporteria post API ventas, caja y reversas

Fecha UTC: 2026-07-02T02:00:41Z

## Alcance

Se ejecuto una fase de auditoria, implementacion bloqueada de lectura y validacion no destructiva para reporterÃ­a post API.

No se ejecutaron operaciones de escritura operativa. No se abrieron turnos, ventas, reversas, cierres, ingresos, retiros ni migraciones.

## Auditoria de reporterÃ­a actual

Hallazgos principales:

- No existia un modulo API formal de reporterÃ­a.
- WPF conserva consultas SQL directas para historial de ventas, detalle de venta, inventario, clientes, saldos liberados, ingreso de caja, retiro de caja y cierre de caja.
- WPF ya usa API para lecturas puntuales de caja, clientes, productos y escrituras controladas segun flags.
- `VentasCrudWindow` lee ventas por SQL directo y ya distingue ventas API efectivo y reversadas mediante `venta_pago`, `movimiento_caja` y `venta_reversa`.
- `DetalleVentaWindow` lee detalle por SQL directo.
- `ClientePage` tiene lectura por API opcional, pero reporte de saldos sigue por SQL directo y exporta archivo local.
- `InventarioWindow` lee inventario por SQL directo y puede exportar PDF.
- `IngresoCajaPage`, `RetirosCajaPage` y `CierreCajaPage` muestran estado de Caja API, pero conservan rutas historicas SQL para registros segun flags.
- `RawPrinterHelper` imprime recibos y cierres historicos; no debe usarse como fuente financiera para reporte neto.

## Riesgos detectados

- Reportes historicos que sumen `ventas.total` sin revisar `venta_reversa` inflan ventas netas.
- Reportes que sumen `venta_pago` sin revisar reversas pueden inflar efectivo neto.
- Reportes que ignoren `movimiento_caja.Reversa` inflan efectivo esperado.
- Reportes que mezclen `cierre_caja` historico con `caja_turno` API sin etiqueta de origen pueden duplicar lectura de cierre.
- Inventario restaurado por reversa no debe interpretarse como ajuste manual.

## Implementacion realizada

Se agrego reporterÃ­a API de solo lectura:

- `GET /api/reportes/ventas/resumen`
- `GET /api/reportes/ventas/detalle`
- `GET /api/reportes/ventas/reversas`
- `GET /api/reportes/caja/resumen`
- `GET /api/reportes/caja/turnos`
- `GET /api/reportes/caja/movimientos`
- `GET /api/reportes/auditoria/inconsistencias`

Componentes:

- `ReportesController`.
- `IReporteService`.
- `ReporteService`.
- `IReporteRepository`.
- `ReporteRepository`.
- Contratos `POS.Api.Contracts.Reportes`.

Todos los endpoints requieren `Reportes.Ver`.

## Validacion no destructiva

Resultados:

- Health API: 200.
- Health base: 200.
- Version API: 200.
- Sin token: 401.
- Token sin `Reportes.Ver`: 403.
- Token con `Reportes.Ver`: todos los GET de reporterÃ­a respondieron 200.

## Integridad

Linea base antes y despues fue igual:

- `ventas`: 1951.
- `DetalleVenta`: 5090.
- `venta_pago`: 13.
- `venta_reversa`: 1.
- `venta_idempotencia`: 14.
- `venta_auditoria`: 14.
- `inventario`: 226.
- stock agregado inventario: 3293.00.
- saldo agregado clientes: -2957962.50.
- `ingreso_caja`: 9.
- `retiro_caja`: 6.
- `cierre_caja`: 15.
- turnos abiertos: 0.
- turnos EnCierre: 0.
- turnos cerrados: 8.
- `movimiento_caja`: 22.
- `caja_idempotencia`: 19.

Confirmado: cero escrituras.


