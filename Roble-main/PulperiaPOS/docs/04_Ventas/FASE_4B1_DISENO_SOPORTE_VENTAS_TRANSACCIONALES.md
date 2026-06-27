# Fase 4B.1 - Diseno de soporte para ventas transaccionales

Fecha UTC: 2026-06-26 10:54:47 UTC

## Alcance

Diseno tecnico y preparacion no ejecutada de scripts aditivos para soportar la futura venta transaccional por `POS.Api`.

No se ejecuto ningun script SQL. No se modifico WPF, base de datos, stock, saldo, caja, cierre, pagos actuales, comprobantes, reportes ni tipo de cambio.

## Hallazgos del esquema actual

Tablas principales existentes:

- `ventas`: encabezado actual de venta; tiene `factura`, `cliente_id`, `total`, `fecha`, `hora`, `usuario_id`, `metodo_pago`, `numero_voucher`, `numero_comprobante`, `monto_pagado`, `vuelto`.
- `DetalleVenta`: detalle actual; tiene `factura`, `producto_id`, `cantidad`, `precio_unitario`.
- `inventario`: fuente de producto, precio y stock actual.
- `cliente`: fuente de saldo actual.
- `usuario`: responsable de venta.
- `ingreso_caja`, `retiro_caja`, `cierre_caja`: caja actual calculada por agregados.
- `TipoCambioDolar`: tipo de cambio diario.

Campos existentes que no deben duplicarse innecesariamente:

- numero de factura: `ventas.factura`.
- metodo principal actual: `ventas.metodo_pago`.
- voucher: `ventas.numero_voucher`.
- comprobante SINPE: `ventas.numero_comprobante`.
- monto pagado y vuelto: `ventas.monto_pagado`, `ventas.vuelto`.
- usuario responsable: `ventas.usuario_id`.

Faltantes para venta API segura:

- idempotencia persistente por usuario y solicitud.
- pagos normalizados por venta.
- auditoria de eventos.
- estructura futura para caja transaccional.

## Decision de diseno

No se modifica `ventas` ni `DetalleVenta` en esta fase. Se preparan tablas de soporte:

```text
venta_idempotencia
venta_pago
venta_auditoria
```

Estas tablas complementan el modelo actual sin competir como fuente de verdad con `ventas` y `DetalleVenta`.

## Diagrama textual propuesto

```text
usuario
  -> venta_idempotencia.usuario_id
  -> venta_pago.usuario_id
  -> venta_auditoria.usuario_id

ventas
  -> venta_idempotencia.factura
  -> venta_pago.factura
  -> venta_auditoria.factura

venta_idempotencia
  -> venta_auditoria.idIdempotencia
```

## Scripts preparados

- `database/diagnostics/007_DiagnosticoSoporteVentasTransaccionales.sql`
- `database/migrations/007_SoporteVentasTransaccionales.sql`
- `database/rollback/007_SoporteVentasTransaccionales_rollback.sql`
- `database/diagnostics/007_ValidacionPostMigracionSoporteVentas.sql`

Todos quedaron preparados sin ejecutarse.

## Compatibilidad historica

El script de migracion es aditivo. No intenta poblar pagos historicos ni auditoria historica. La futura API registrara nuevas ventas con las tablas de soporte a partir de su activacion.

## Contrato futuro resumido

WPF podra enviar:

- `clienteId`;
- productos y cantidades;
- intencion de pago;
- monto recibido;
- referencia o voucher;
- tipo de cambio observado;
- `IdempotencyKey`;
- observaciones opcionales.

API debe recalcular:

- precio final;
- total;
- stock disponible;
- saldo disponible;
- vuelto;
- estado de venta;
- usuario autenticado;
- fecha/hora UTC.

WPF jamas debe decidir como autoridad final:

- precio;
- stock;
- total definitivo;
- saldo disponible;
- permisos;
- identidad;
- fecha/hora oficial;
- estado de pago;
- estado de caja.

## Riesgos controlados por el diseno

- Doble clic o reintento: `venta_idempotencia`.
- Pagos multiples futuros: `venta_pago`.
- Trazabilidad: `venta_auditoria`.
- Caja futura: datos de pago y auditoria listos para vincular con `CajaTurno` y `MovimientoCaja`.

## Recomendacion

Siguiente fase recomendada: Fase 4B.2, ejecutar solamente el diagnostico `007_DiagnosticoSoporteVentasTransaccionales.sql`, revisar resultados agregados y aprobar o ajustar el script de migracion antes de aplicarlo.
