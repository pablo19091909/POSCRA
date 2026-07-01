# Decisiones y limitaciones Caja API

Fecha UTC: 2026-07-01 02:46:50 UTC

## Decisiones

- Caja API usa `caja_turno`, `movimiento_caja` y `caja_idempotencia` como modelo nuevo.
- Los cierres exactos no crean `CierreDiferencia`.
- Los cierres con sobrante o faltante guardan el signo en `caja_turno.diferencia`.
- `movimiento_caja.monto` para `CierreDiferencia` guarda el valor absoluto positivo.
- Las rutas historicas permanecen disponibles hasta corte formal.
- Los flags de escritura quedan apagados por defecto.

## Limitaciones pendientes

- Reversas inmutables no implementadas.
- Ventas en efectivo aun no integradas a `VentaEfectivo` de Caja API.
- Dolares fuera de alcance.
- Donacion fuera de alcance.
- Pagos combinados fuera de alcance.
- Auditoria de saldo historico negativo de clientes pendiente.
- Produccion no autorizada.

## Riesgos

- Mientras ventas efectivo no escriban `movimiento_caja`, Caja API no representa todo el efectivo operativo de ventas.
- El corte sin fecha formal puede mezclar habitos operativos de caja historica y caja API.
- Reversiones manuales o SQL directo deben permanecer prohibidas para no romper auditoria.

## Siguiente recomendacion

Pasar a una fase fuera de Caja API para integrar ventas en efectivo con movimientos de caja, manteniendo el mismo patron: feature flag, Environment=Test, idempotencia, una ruta a la vez y validaciones manuales con compuertas.
