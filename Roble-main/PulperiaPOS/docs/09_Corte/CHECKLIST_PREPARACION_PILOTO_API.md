# Checklist preparacion piloto API

## Antes del piloto

- ReporterÃa API validada con datos Test.
- Reversas API validadas.
- Caja API conciliada.
- Inventario restaurado por reversa confirmado.
- Permisos minimos definidos.
- Usuarios piloto capacitados.
- Respaldo verificable definido.
- Protocolo de incidentes documentado.
- Plan de reversiÃ³n operacional listo.

## Durante el piloto

- Activar escritura API solo a usuarios piloto.
- Monitorear ventas, reversas, caja e idempotencias.
- Revisar inconsistencias diariamente.
- Comparar ventas brutas, reversadas y netas.
- Validar efectivo bruto, reversas y neto.
- Confirmar que no hay doble conteo.

## Criterios de salida

- Sin idempotencias pendientes.
- Sin reversas huerfanas.
- Sin movimientos `VentaEfectivo` huerfanos.
- Cierres conciliados.
- Usuarios piloto sin bloqueos operativos.
- ReporterÃa aceptada por administracion.

## No pasar a produccion si

- Hay diferencias no explicadas.
- No existe respaldo.
- No hay responsable de soporte.
- No se probo recuperacion.
- No hay capacitacion.
- No hay permisos minimos.


