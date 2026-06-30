# Plan de migracion gradual WPF Caja API

Fecha/hora UTC: 2026-06-30 02:13:13 UTC

## Orden obligatorio

1. Lectura de turno y pre-cierre.
2. Apertura de turno.
3. Ingreso de caja.
4. Retiro de caja.
5. Cierre de turno.
6. Reporte por turno.
7. Retiro definitivo de ruta historica con fecha de corte formal.

## Etapa 1 - Lectura

- Flag: `UseCajaApiRead`.
- Endpoints: `GET /api/caja/turnos/abierto`, `GET /api/caja/turnos/{id}/movimientos`, `GET /api/caja/turnos/{id}/pre-cierre`.
- Permiso: `Caja.Ver`.
- Prueba Test: sin turno abierto debe mostrar estado claro.
- Criterio de salida: lectura estable sin escrituras ni fallback SQL.

## Etapa 2 - Apertura

- Flag: `UseCajaApiOpenWrite`.
- Endpoint: `POST /api/caja/turnos/abrir`.
- Permiso: `Caja.Abrir`.
- Prueba Test: abrir un turno unico con fondo inicial.
- Criterio de salida: rechazo correcto si ya existe turno abierto.
- Si API falla: error seguro, sin SQL historico.

## Etapa 3 - Ingreso

- Flag: `UseCajaApiIngresoWrite`.
- Endpoint: `POST /api/caja/ingresos`.
- Permiso: `Caja.Ingresar`.
- Prueba Test: ingreso idempotente con reintento.
- Criterio de salida: una sola idempotencia y un solo movimiento.
- Si API falla: reintento con misma intencion, sin `ingreso_caja`.

## Etapa 4 - Retiro

- Flag: `UseCajaApiRetiroWrite`.
- Endpoint: `POST /api/caja/retiros`.
- Permiso: `Caja.Retirar`.
- Prueba Test: retiro idempotente y control de efectivo disponible.
- Criterio de salida: rechazo seguro por saldo insuficiente.
- Si API falla: reintento con misma intencion, sin `retiro_caja`.

## Etapa 5 - Cierre

- Flag: `UseCajaApiCierreWrite`.
- Endpoint: `POST /api/caja/turnos/{id}/cerrar`.
- Permiso: `Caja.Cerrar`.
- Prueba Test: cierre exacto, sobrante y faltante.
- Criterio de salida: `rowVersion`, idempotencia y diferencia validados desde UI.
- Si API falla: no insertar en `cierre_caja`.

## Etapa 6 - Reporte por turno

- Flag futuro a definir si hace falta.
- Endpoints: lecturas de turno, movimientos y resumen.
- Permiso: `Caja.VerResumen`.
- Prueba Test: reporte consistente con movimientos del turno.
- Criterio de salida: reporte no depende de tablas historicas.

## Etapa 7 - Corte formal

- Requisito: aprobacion operativa y fecha de corte.
- Accion: retirar ruta historica solo despues de validaciones completas.
- Prohibicion: no mezclar operaciones API e historicas para una misma accion.

## Limites pendientes

- Sin reversas.
- Sin ventas API en efectivo integradas a caja desde WPF.
- Sin dolares.
- Sin donacion.
- Sin pagos combinados.
- Sin retiro definitivo de SQL historico.

