# Plan corte gradual Caja historica a Caja API

Fecha UTC: 2026-07-01 02:46:50 UTC

## Decision propuesta

No retirar todavia las rutas historicas. Mantenerlas disponibles para consulta y contingencia controlada hasta definir fecha formal de corte.

## Orden de activacion futura

1. `UseCajaApiRead`
2. `UseCajaApiOpenWrite`
3. `UseCajaApiIngresoWrite`
4. `UseCajaApiRetiroWrite`
5. `UseCajaApiCierreWrite`

## Reglas de activacion

- Activar un flujo a la vez.
- Exigir prueba manual y evidencia posterior por cada flujo.
- Mantener `EnableCajaApiWrite=false` por defecto fuera de ventanas controladas.
- No permitir fallback SQL silencioso.
- No permitir dual write.
- No imprimir comprobantes historicos desde rutas API.
- Mantener caja historica para periodos previos.
- No borrar cajas, cierres, ingresos ni retiros historicos.
- No activar en produccion sin decision operacional formal.

## Criterios antes del corte

- Definir fecha y hora formal de corte.
- Confirmar usuarios/roles operativos.
- Confirmar respaldo y plan de contingencia.
- Confirmar monitoreo de health checks.
- Confirmar procedimiento de reapertura o correccion futura mediante reversas, no por edicion directa.
- Integrar ventas en efectivo con `VentaEfectivo` antes de considerar caja API como fuente completa de efectivo operativo.

## Estado recomendado actual

Caja API queda validada en Test para apertura, ingreso, retiro, lectura/pre-cierre y cierre desde WPF. Aun no debe activarse como unico flujo productivo.
