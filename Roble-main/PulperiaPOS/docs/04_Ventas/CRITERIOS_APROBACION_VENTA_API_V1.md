# Criterios aprobacion venta API V1

## Estado actual

No aprobado.

La Fase 4C.3 no pudo ejecutarse porque no se valido ambiente aislado no productivo.

## Criterios minimos para aprobar

- Ambiente identificado como no productivo.
- Conexion de POS.Api separada de produccion.
- Datos sinteticos o sanitizados.
- `EnableVentasApiWrite=true` solo en configuracion local no versionada del ambiente de prueba.
- Casos exitosos A-E completados.
- Casos de fallo F-L con rollback total.
- Casos de idempotencia M-N correctos.
- Caso de concurrencia O correcto.
- Cero stock negativo.
- Cero saldo negativo.
- Cero ventas duplicadas por idempotencia.
- Cero pagos, auditorias o detalles huerfanos.
- Caja sin cambios hasta implementar CajaTurno/MovimientoCaja.
- Dolares bloqueado.
- Donacion bloqueada.
- WPF sin modificaciones.
- Produccion sin uso ni cambios.

## Criterio de salida

La Venta API V1 solo puede considerarse aprobada para integracion WPF cuando todos los criterios anteriores pasen en ambiente no productivo y queden documentados con agregados antes/despues.

## Recomendacion actual

No avanzar a integracion WPF.

Preparar ambiente de prueba aislado y repetir Fase 4C.3.
