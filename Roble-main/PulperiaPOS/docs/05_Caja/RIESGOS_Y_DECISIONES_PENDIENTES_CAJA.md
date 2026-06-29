# Riesgos y decisiones pendientes de caja

## Riesgos

| Severidad | Riesgo | Mitigacion propuesta |
| --- | --- | --- |
| Critico | Caja sin movimiento transaccional por venta. | Crear `movimiento_caja` dentro de la misma transaccion de venta API. |
| Critico | Cierre basado en agregados por fecha/hora. | Migrar a cierre por `caja_turno`. |
| Critico | Retiro valida disponible fuera de transaccion. | Endpoint API con validacion y registro atomicos. |
| Critico | Venta concurrente durante cierre. | Estado `EnCierre` y bloqueo transaccional. |
| Alto | Falta turno abierto. | Venta API debe fallar si no hay turno abierto. |
| Alto | Ingresos/retiros editables o borrables en futuro. | Movimiento inmutable y reversos. |
| Alto | Diferencia de caja sin politica formal. | Capturar contado, diferencia, tolerancia y observacion. |
| Alto | Transicion SQL historica/API. | Mantener historicos intactos y reportes mixtos temporales. |
| Medio | Vuelto de efectivo puede duplicarse si se modela mal. | Movimiento unico por total neto de venta. |
| Medio | Cliente General con `id=0`. | Regla explicita de contrato y bloqueo de `SaldoCliente`. |
| Medio | Dolares sin politica de caja. | Mantener bloqueado hasta definir moneda/tipo de cambio operativo. |
| Medio | Donacion sin tratamiento propio. | Mantener bloqueado en Venta API V1. |
| Medio | Pagos combinados. | Mantener fuera de alcance hasta diseno de pagos multiples. |
| Medio | Conciliacion tarjeta/SINPE. | Disenar conciliacion separada de efectivo fisico. |
| Medio | Anulacion/devolucion. | Reversos financieros y auditoria antes de habilitar. |
| Bajo | Reportes existentes no conocen turno. | Compatibilidad temporal y nuevos reportes por turno. |

## Decisiones pendientes

- Codigo de caja logica inicial: si sera fijo por equipo, por sucursal o configurable.
- Permisos exactos: `Caja.Abrir`, `Caja.Ingresar`, `Caja.Retirar`, `Caja.Cerrar`, `Caja.CerrarConDiferencia`, `Caja.VerResumen`.
- Tolerancia de diferencia de caja.
- Politica de anulacion de turno.
- Tratamiento final de `cierre_caja` historico.
- Conciliacion bancaria para tarjeta y SINPE.
- Tratamiento de Dolares y tipo de cambio operativo.
- Modelo de devoluciones y anulaciones API.
