# Plan de migracion de ventas API por fases

Fecha UTC: 2026-06-26 10:54:47 UTC

## Fase 4B.1

Preparar diseno, scripts y documentacion. No ejecutar scripts. No modificar WPF ni base.

Estado: preparado.

## Fase 4B.2

Ejecutar diagnostico:

- `database/diagnostics/007_DiagnosticoSoporteVentasTransaccionales.sql`

Revisar:

- ventas sin detalle;
- detalles sin venta;
- ventas sin usuario;
- ventas sin cliente;
- totales nulos o invalidos;
- stock negativo;
- saldo negativo;
- dependencias de reportes o procedimientos;
- impacto de constraints propuestas.

Resultado esperado: aprobacion o ajuste de migracion.

## Fase 4B.3

Aplicar esquema de soporte:

- `database/migrations/007_SoporteVentasTransaccionales.sql`

No activar ventas API todavia.

Ejecutar luego:

- `database/diagnostics/007_ValidacionPostMigracionSoporteVentas.sql`

## Fase 4C

Implementar `POST /api/ventas` en modo preparado/no activado:

- controller;
- request/response;
- servicio transaccional;
- repositorio;
- idempotencia;
- pagos;
- auditoria;
- validaciones de stock y saldo condicionadas.

No conectar WPF aun por defecto.

## Fase 4D

Pruebas transaccionales contra base de prueba:

- efectivo exacto;
- efectivo con vuelto;
- tarjeta;
- SINPE;
- dolares;
- saldo cliente;
- stock insuficiente;
- saldo insuficiente;
- doble clic;
- reintento con idempotencia;
- concurrencia sobre ultimo producto;
- error durante detalle, stock, saldo y pago.

## Fase 4E

Integrar `VentasPage` con feature flag reversible:

- WPF envia intencion de venta;
- API recalcula;
- SQL directo queda como rollback temporal;
- no fallback silencioso si flag API esta activo.

## Fase 4F

Validacion controlada de venta API:

- una venta de prueba aprobada;
- monitoreo de stock, saldo, pagos, auditoria e idempotencia;
- rollback funcional por feature flag.

## Fase Caja

Disenar e implementar:

- `CajaTurno`;
- `MovimientoCaja`;
- cierre auditable;
- bloqueo o regla explicita para ventas despues de cierre.

## Fase Anulacion/Devolucion

Sustituir borrado fisico por:

- anulacion;
- devolucion;
- movimientos inversos de stock/saldo/caja;
- auditoria obligatoria;
- permisos especificos.

## Criterios de avance

No avanzar a `POST /api/ventas` hasta que:

- el diagnostico 4B.2 este revisado;
- la migracion 4B.3 este aplicada y validada;
- exista acuerdo sobre reglas de pago y caja;
- se defina politica de idempotencia y retencion.
