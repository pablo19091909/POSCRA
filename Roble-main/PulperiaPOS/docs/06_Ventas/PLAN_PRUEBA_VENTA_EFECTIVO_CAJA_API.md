# Plan prueba VentaEfectivo Caja API

## Precondiciones

- `Environment=Test`.
- `writes_allowed_for_testing=1`.
- Un turno `CAJA_PRINCIPAL_TEST` abierto por Caja API.
- Producto de prueba con stock suficiente.
- Cliente valido.
- Usuario con `Ventas.Crear`.
- Todos los flags apagados antes de iniciar.

## Pruebas no destructivas previas

1. `POST /api/ventas` sin token: `401`.
2. Token sin `Ventas.Crear`: `403`.
3. Token autorizado con `EnableVentasApiWrite=false`: rechazo seguro.
4. Token autorizado con `EnableCajaApiWrite=false`: rechazo seguro para efectivo.
5. Token autorizado con `EnableVentasApiEfectivoCajaWrite=false`: rechazo seguro para efectivo.
6. Confirmar cero cambios de agregados.

## Primera prueba destructiva futura

Solo en Prompt Maestro 2:

1. Abrir turno Test.
2. Activar flags requeridos.
3. Crear venta efectiva de monto pequeno desde API/WPF autorizado.
4. Validar:
   - 1 venta;
   - 1 detalle por item;
   - 1 pago efectivo;
   - 1 `VentaEfectivo`;
   - 1 auditoria;
   - 1 idempotencia completada;
   - inventario descontado una vez;
   - efectivo esperado aumentado por el monto de venta.
5. Repetir misma key y mismo request.
6. Repetir misma key con request distinto.
7. Apagar flags y cerrar API.

## No permitido

- No usar SQL manual para crear ventas.
- No crear movimiento de caja fuera de la transaccion.
- No usar fallback SQL historico.
- No probar produccion.
