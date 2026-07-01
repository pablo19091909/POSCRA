# Fase 5A.3 - Prueba API venta efectivo con Caja Test

Fecha UTC: 2026-07-01 03:27:11 UTC

## Autorizacion y alcance

Se ejecuto una prueba controlada en `Environment=Test` con `writes_allowed_for_testing=1`.

La prueba autorizo exclusivamente:

- un turno Test nuevo abierto para la caja de prueba;
- un movimiento `FondoInicial`;
- una venta sintetica por POS.Api pagada completamente en efectivo;
- un detalle de venta, un pago, auditoria e idempotencia de venta;
- un movimiento `VentaEfectivo` asociado a la venta;
- reintento idempotente, conflicto de misma llave con intencion distinta y una validacion de rollback.

No se uso WPF. No se ejecutaron escrituras SQL manuales. No se ejecutaron scripts de insercion, actualizacion, rollback ni migraciones.

## Linea base

Antes de habilitar la ventana temporal:

- Ambiente Test autorizado: confirmado.
- Turnos abiertos de la caja Test: 0.
- Turnos en cierre de la caja Test: 0.
- Idempotencias pendientes/fallidas de caja: 0.
- Idempotencias pendientes/fallidas de venta: 0.
- `ventas`: 1948.
- `DetalleVenta`: 5087.
- `venta_pago`: 10.
- `venta_idempotencia`: 10.
- `venta_auditoria`: 10.
- `movimiento_caja`: 16.
- `caja_turno`: 6.
- `caja_idempotencia`: 15.
- `ingreso_caja`: 9.
- `retiro_caja`: 6.
- `cierre_caja`: 15.

Producto elegido: producto sintetico de stock alto con precio valido y stock suficiente. No se documenta su identificador interno.

Cliente elegido: cliente sintetico autorizado para pago en efectivo. No se documenta su identificador interno.

## Compuerta previa

Con flags de escritura apagados:

- `/health`: 200.
- `/health/database`: 200.
- `/api/system/version`: 200.
- `POST /api/ventas` sin token: 401.
- `POST /api/ventas` con token sin permiso: 403.
- `POST /api/ventas` autorizado con flags apagados: 503 seguro.

## Ventana temporal

Se habilitaron solo en el proceso temporal de POS.Api:

- `EnableCajaApiWrite=true`.
- `EnableVentasApiWrite=true`.
- `EnableVentasApiEfectivoCajaWrite=true`.
- `EnableLegacyHashUpgrade=false`.

WPF permanecio con escrituras apagadas:

- `UseCajaApiOpenWrite=false`.
- `UseCajaApiIngresoWrite=false`.
- `UseCajaApiRetiroWrite=false`.
- `UseCajaApiCierreWrite=false`.
- `UseVentasApiWrite=false`.
- `UseVentasApiEfectivoWrite=false`.

## Resultado

- Apertura de turno Test por API: 200.
- Venta API en efectivo: 200.
- Resultado de venta: nueva.
- Total de venta: 10.00.
- Reintento idempotente con misma llave y mismo cuerpo: 200, repetida.
- Misma llave con intencion distinta: 409.
- Solicitud invalida controlada para rollback: 400.

## Integridad posterior

Variaciones autorizadas:

- `ventas`: +1.
- `DetalleVenta`: +1.
- `venta_pago`: +1.
- `venta_idempotencia`: +1.
- `venta_auditoria`: +1.
- `inventario`: -1 unidad del producto sintetico elegido.
- `movimiento_caja`: +2.
- `caja_turno`: +1.
- `caja_idempotencia`: +1 por apertura de turno.

Sin cambios:

- `ingreso_caja`: 0 variacion.
- `retiro_caja`: 0 variacion.
- `cierre_caja`: 0 variacion.

Estado final:

- Existe exactamente un turno abierto de la caja Test.
- No existe turno en cierre.
- El turno se mantiene abierto como evidencia para el Prompt Maestro 3.

## Riesgos pendientes

- WPF todavia no usa la ruta nueva de venta efectiva.
- Reversas inmutables no estan implementadas.
- Pagos no efectivos quedan fuera de esta fase.
- Produccion no esta autorizada.
- La caja logica de la venta sigue resuelta por servidor para Test; antes de escenarios multi-caja debe definirse el contrato final.

## Recomendacion

Avanzar al Prompt Maestro 3 para validar la integracion WPF de venta en efectivo contra la ruta API ya probada, manteniendo el turno Test abierto y las escrituras apagadas por defecto.
