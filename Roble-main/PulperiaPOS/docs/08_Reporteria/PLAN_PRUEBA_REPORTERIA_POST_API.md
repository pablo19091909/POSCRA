# Plan de prueba reporterÃ­a post API

## Objetivo

Validar que la reporterÃ­a API refleja ventas brutas, reversadas, netas, caja e inconsistencias sin modificar datos.

## Preparacion

- Confirmar flags de escritura apagadas.
- Confirmar health API y base en 200.
- Confirmar `UseCajaApiRead=true`.
- Confirmar cero turnos abiertos inesperados.

## Seguridad

Pruebas realizadas:

- Sin token: 401.
- Token sin `Reportes.Ver`: 403.
- Token con `Reportes.Ver`: GET exitosos.

## GETs validados

- ventas resumen: 200.
- ventas detalle: 200.
- ventas reversas: 200.
- caja resumen: 200.
- caja turnos: 200.
- caja movimientos: 200.
- auditoria inconsistencias: 200.

## Validaciones financieras

- Venta reversada aparece y no desaparece.
- Reversa cuenta una vez.
- Efectivo bruto usa `VentaEfectivo`.
- Efectivo neto resta `Reversa`.
- Turno de prueba cerrado conserva esperado 1000.00, contado 1000.00 y diferencia 0.00.
- `CierreDiferencia` queda separado del efectivo esperado.

## Validacion de integridad

Comparar antes y despues:

- ventas;
- detalle;
- pagos;
- reversas;
- idempotencias;
- auditorias;
- inventario agregado;
- saldo agregado;
- caja;
- ingresos, retiros y cierres historicos.

Resultado de esta fase: sin cambios.


