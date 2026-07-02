# Plan de prueba - Reversa de venta efectivo con Caja API

## Precondiciones futuras

- Ambiente `Test`.
- POS.Api en HTTPS.
- Health checks en 200.
- Usuario con `Ventas.Reversar`.
- Venta elegible creada en turno abierto.
- Pago efectivo total.
- Movimiento `VentaEfectivo` confirmado.
- Inventario verificable.
- Efectivo esperado suficiente.

## Pruebas no destructivas actuales

- Sin token: debe responder 401.
- Token sin permiso: debe responder 403.
- Token con permiso y flags apagados: debe responder 503 seguro.
- Health publico: debe responder 200.
- Agregados antes y despues: deben permanecer iguales.

## Pruebas destructivas futuras controladas

1. Abrir turno Test.
2. Crear venta efectivo elegible por API.
3. Registrar linea base agregada.
4. Ejecutar reversa una vez.
5. Reintentar misma key.
6. Reintentar key distinta para la misma venta.
7. Verificar inventario restaurado.
8. Verificar movimiento compensatorio de caja.
9. Verificar auditoria.
10. Cerrar turno exacto.

## Criterio de exito futuro

La reversa debe quedar confirmada una sola vez, sin borrar la venta original, sin duplicar caja, sin duplicar inventario y con cierre exacto.
