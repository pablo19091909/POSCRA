# Validacion movimientos y no fallback post retiro

Fecha UTC: 2026-06-30

## Movimientos del turno abierto

El turno abierto conserva:

- 1 movimiento `FondoInicial`.
- 1 movimiento `IngresoCaja` API de 100.00.
- 1 movimiento `RetiroCaja` API de 100.00.
- 0 ajustes.
- 0 reversas.
- 0 `CierreDiferencia` en el turno abierto.

## Idempotencia

Estado agregado:

- `AbrirTurno:Completada`: 1.
- `IngresoCaja:Completada`: 3.
- `RetiroCaja:Completada`: 3.
- `CerrarTurno:Completada`: 3.
- `EnProceso`: 0.
- Fallidas relacionadas con el turno abierto: 0.

No se crearon idempotencias durante esta fase.

## No fallback SQL

Revision estatica:

- `CajaApiReadStatusViewHelper` usa `CajaApiClient` para lectura de estado/resumen cuando `UseCajaApiRead=true`.
- `CajaApiClient` bloquea escrituras cuando los flags correspondientes estan apagados.
- `IngresoCajaPage` y `RetirosCajaPage` conservan rutas historicas para registro cuando escritura API esta apagada, pero la fase no presiono botones de escritura.
- `CierreCajaPage` conserva dependencia historica de `CajaHelper` y `DBConnection`; no esta migrada a lectura API.

Conclusion:

- No hubo fallback SQL ejecutado durante la validacion de API caida.
- No hubo escrituras.
- La migracion de lectura de `CierreCajaPage` queda como requisito tecnico siguiente.

## Tablas historicas

Sin cambios:

- `ingreso_caja`: 9.
- `retiro_caja`: 6.
- `cierre_caja`: 15.
- `ventas`: 1948.
- `venta_pago`: 10.
- `venta_idempotencia`: 10.
- Inventario agregado: 3296.00.
- Saldo agregado clientes: -2957962.50.
