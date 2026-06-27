# Fase 4B.3 - Migracion 007 de soporte ventas transaccionales aplicada

Fecha/hora UTC: 2026-06-26 11:28:58 UTC

## Autorizacion del operador

El operador confirmo respaldo valido o recuperacion point-in-time disponible, checklist completo y autorizacion explicita para ejecutar la migracion 007.

## Alcance ejecutado

Script ejecutado una unica vez:

`database/migrations/007_SoporteVentasTransaccionales.sql`

Resultado: `SUCCESS`.

No se ejecuto rollback. No se ejecutaron scripts alternativos. No se hizo backfill de ventas, pagos, auditorias ni idempotencias historicas.

## Validaciones previas

Endpoints previos:

| Endpoint | Resultado |
| --- | --- |
| `/health` | HTTP 200 |
| `/health/database` | HTTP 200 |
| `/api/system/version` | HTTP 200 |

Precondiciones de base:

| Metrica | Antes |
| --- | ---: |
| Tablas soporte existentes | 0 |
| Ventas historicas | 1881 |
| Detalles historicos | 4921 |
| Ventas sin detalle | 0 |
| Detalles sin venta | 0 |
| Registros inventario | 220 |
| Stock agregado | 2980 |
| Clientes | 162 |
| Ingresos caja | 9 |
| Retiros caja | 6 |
| Cierres caja | 15 |

La validacion estatica del script confirmo:

- Solo crea tablas de soporte, FKs, indices, constraints y defaults.
- No contiene `INSERT`, `UPDATE`, `DELETE`, `MERGE` ni `TRUNCATE`.
- No altera tablas historicas.
- No contiene `DROP TABLE`.
- Incluye `Donación` en `CK_venta_pago_metodo`.

## Tablas creadas

- `dbo.venta_idempotencia`
- `dbo.venta_pago`
- `dbo.venta_auditoria`

Las tres tablas quedaron vacias despues de la migracion.

## `venta_idempotencia`

Validado:

- PK `PK_venta_idempotencia`.
- Indice unico `UX_venta_idempotencia_usuario_key` por `usuario_id` + `idempotency_key`.
- Indice `IX_venta_idempotencia_estado_expira`.
- FK a `dbo.usuario`.
- FK segura a `dbo.ventas.factura`.
- `factura` permite `NULL`.
- Estados permitidos: `EnProceso`, `Completada`, `Fallida`.
- Campos UTC en `datetime2`.
- `request_hash` es `varbinary(32)`.
- Tabla vacia.

## `venta_pago`

Validado:

- PK `PK_venta_pago`.
- FK segura a `dbo.ventas.factura`.
- FK a `dbo.usuario`.
- Indices `IX_venta_pago_factura` y `IX_venta_pago_metodo_fecha`.
- Montos en `decimal`.
- `monto` exige valor positivo.
- `monto_recibido` permite `NULL`.
- `vuelto` permite `NULL` y no permite valores negativos cuando existe.
- `tipo_cambio_aplicado` debe ser positivo cuando existe.
- Tabla vacia.

Metodos aceptados por `CK_venta_pago_metodo`:

- `Efectivo`
- `Tarjeta`
- `Sinpe`
- `Dolares`
- `SaldoCliente`
- `Donación`

## `venta_auditoria`

Validado:

- PK `PK_venta_auditoria`.
- FK segura a `dbo.ventas.factura`.
- FK a `dbo.venta_idempotencia`.
- FK a `dbo.usuario`.
- Indices `IX_venta_auditoria_factura_fecha` y `IX_venta_auditoria_evento_fecha`.
- Contiene evento, usuario, fecha UTC y referencia de venta.
- `detalle_json` se valida con `ISJSON`.
- Tabla vacia.

## Integridad historica posterior

| Metrica | Despues |
| --- | ---: |
| Ventas historicas | 1881 |
| Detalles historicos | 4921 |
| Ventas sin detalle | 0 |
| Detalles sin venta | 0 |
| Registros inventario | 220 |
| Stock agregado | 2980 |
| Clientes | 162 |
| Ingresos caja | 9 |
| Retiros caja | 6 |
| Cierres caja | 15 |
| `venta_idempotencia` registros | 0 |
| `venta_pago` registros | 0 |
| `venta_auditoria` registros | 0 |

Confirmacion: no hubo nuevas ventas, no hubo cambios en `DetalleVenta`, no hubo cambios en inventario, saldo agregado de clientes, caja ni cierres.

## Compatibilidad tecnica

Compilacion completa:

- `PulperiaPOS`: correcta, 0 errores.
- `POS.Api`: correcta, 0 errores.

Endpoints posteriores:

| Endpoint | Resultado |
| --- | --- |
| `/health` | HTTP 200 |
| `/health/database` | HTTP 200 |
| `/api/system/version` | HTTP 200 |

La instancia local de API usada para validacion fue cerrada y el puerto `7046` quedo libre.

## Cambios no realizados

- No se modifico WPF.
- No se modifico `VentasPage`.
- No se modifico `DBConnection.cs`.
- No se cambiaron feature flags.
- No se creo `POST /api/ventas`.
- No se hizo login de prueba.
- No se registraron ventas de prueba.
- No se modificaron ventas historicas, detalle, inventario, cliente, usuario, caja, cierres, comprobantes, donaciones, reportes ni tipo de cambio.

## Riesgos pendientes

- Aun no existe endpoint transaccional de ventas en POS.Api.
- WPF sigue procesando ventas por SQL directo.
- Las tablas nuevas estan listas, pero sin uso funcional hasta una fase posterior.
- El rollback solo debe considerarse manualmente y nunca si las tablas de soporte contienen registros.

## Siguiente fase recomendada

Fase 4C: disenar e implementar el contrato inicial de ventas transaccionales en POS.Api, empezando por request/response, permisos, idempotencia y validaciones server-side, sin conectar todavia WPF a escritura API hasta completar pruebas controladas.
