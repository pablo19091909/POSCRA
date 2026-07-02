# Plan de corte gradual - ventas historicas hacia API

## Etapa 1 - Observacion

Objetivo:

- Lectura API habilitada.
- Escritura historica sigue disponible.
- Indicadores de origen visibles.
- Reporte comparativo disponible.

Criterios de salida:

- ReporterÃ­a API validada.
- Diferencias explicadas.
- Usuarios piloto identificados.

## Etapa 2 - Venta efectiva API controlada

Objetivo:

- Venta efectiva por API solo para usuarios piloto.
- Ruta historica visible con advertencia.
- Monitoreo de idempotencia, caja e inventario.

Criterios de salida:

- Ventas piloto conciliadas.
- Sin idempotencias pendientes.
- Caja e inventario consistentes.

## Etapa 3 - Reversa API obligatoria para ventas API

Objetivo:

- Ventas API no se borran fisicamente.
- Reversa API es unica accion para ventas API.
- Borrado historico bloqueado para documentos API.

Criterios de salida:

- Doble reversa bloqueada.
- Inventario restaurado.
- Movimiento compensatorio registrado.

## Etapa 4 - Restriccion de escritura historica

Objetivo:

- Nuevas ventas efectivas ya no se crean por SQL historico.
- Nuevas reversas efectivas ya no se crean por SQL historico.
- Historicos siguen disponibles en consulta.

Criterios de salida:

- Soporte operativo listo.
- Reportes netos validados por periodo.
- Permisos minimos definidos.

## Etapa 5 - Corte formal

Requisitos:

- fecha de corte;
- respaldo verificable;
- checklist de usuarios;
- monitoreo;
- protocolo de incidentes;
- plan de reversiÃ³n operacional;
- responsables;
- capacitacion;
- piloto aprobado.

No recomendar produccion hasta completar restauracion o recuperacion validada segun infraestructura real.


