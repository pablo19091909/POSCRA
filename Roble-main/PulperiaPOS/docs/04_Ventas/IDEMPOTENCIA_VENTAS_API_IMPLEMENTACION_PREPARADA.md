# Idempotencia ventas API - implementacion preparada

## Tabla

`dbo.venta_idempotencia`

La tabla existe desde la migracion 007. En Fase 4C.1 no se insertan ni actualizan registros.

## Clave

La busqueda preparada usa:

`usuario_id + idempotency_key`

## Hash

El hash preparado usa SHA-256 deterministico sobre campos de intencion de venta:

- Cliente.
- Items.
- Pago.
- Observaciones.
- Tipo de cambio observado.
- Referencia o voucher.

No incluye:

- Token.
- Claims completos.
- Connection strings.
- Secretos.
- Datos internos de seguridad.
- Factura futura.
- Fecha/hora oficial futura.

## Estados futuros

- Nueva solicitud.
- Solicitud en proceso.
- Solicitud completada.
- Solicitud fallida.
- Misma clave con request distinto.

## Respuestas futuras

- Nueva venta creada.
- Repeticion segura de venta completada.
- Conflicto por misma clave con request distinto.
- Solicitud en proceso.
- Fallo anterior recuperable o no recuperable.

## Estado Fase 4C.1

Solo existe lectura preparada y calculo de hash. Con `EnableVentasApiWrite=false`, el endpoint corta antes de consultar o escribir idempotencia.
