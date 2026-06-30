# Fase 4F.14 - Auditoria y RetiroCaja API bloqueado

Fecha UTC: 2026-06-29 16:07:09 UTC

## Alcance

Se audito y preparo tecnicamente `POST /api/caja/retiros` para un flujo futuro idempotente, sin activar escrituras ni crear datos.

No se crearon retiros, movimientos, idempotencias, cierres, ajustes, reversas, ventas ni cambios historicos.

## Auditoria de retiro historico

El flujo historico vive en `PulperiaPOS/Views/RetirosCajaPage.xaml.cs` y usa `CajaHelper`.

Hallazgos:

- La disponibilidad se calcula antes del insert desde agregados historicos.
- El calculo usa ventas efectivo, `ingreso_caja` y `retiro_caja`.
- Usa `double`, no `decimal`.
- La validacion de disponible ocurre fuera de una transaccion de escritura.
- Dos operadores podrian calcular el mismo disponible y retirar de forma concurrente.
- El retiro se inserta directamente en `retiro_caja`.
- `retiro_caja` historico contiene `idRetiro`, `monto`, `motivo`, `fecha`, `hora`.
- El usuario se imprime en recibo, pero no queda atribuido en la tabla historica.
- Fecha y hora salen de hora local del cliente.
- No hay turno operacional asociado.
- No hay idempotencia ni proteccion contra reintento.

Decision:

`retiro_caja` no debe reutilizarse como destino de Caja API. Caja API debe usar `movimiento_caja` por turno, UTC, usuario autenticado e idempotencia persistente.

## Modelo real validado

Constraints reales confirmados:

- `movimiento_caja.tipo_movimiento`: incluye `FondoInicial`, `VentaEfectivo`, `IngresoCaja`, `RetiroCaja`, `AjustePositivo`, `AjusteNegativo`, `DevolucionEfectivo`, `CierreDiferencia`, `Reversa`.
- `movimiento_caja.origen`: incluye `POS.Api`, `WPF`, `MigracionFutura`, `AjusteManual`.
- `movimiento_caja.estado`: `Confirmado`, `Reversado`.
- `movimiento_caja.moneda`: `CRC`, `USD`.
- `caja_idempotencia.operacion`: incluye `IngresoCaja`, `RetiroCaja`, `CerrarTurno`, `AjusteCaja`, `ReversaMovimiento`.
- `caja_idempotencia.estado`: `EnProceso`, `Completada`, `Fallida`.

Estado actual Test:

- Turno abierto Test: `1`.
- Movimientos de caja: `3`.
- Retiros API: `0`.
- Cierres API: `0`.
- Idempotencias totales: `2`.
- Idempotencias `RetiroCaja`: `0`.
- Efectivo esperado: `1501.00`.

## Preparacion realizada

Se preparo el contrato futuro de retiro:

- `RegistrarRetiroCajaRequest` ahora incluye `CajaCodigo`.
- `CajaController` pasa el header `Idempotency-Key` al servicio.
- `ICajaService` y `CajaService` aceptan el header para retiro.
- `ICajaIdempotencyService` y `CajaIdempotencyService` calculan hash canonico para `RetiroCaja`.

El endpoint permanece bloqueado:

- con `EnableCajaApiWrite=false`, responde `503` antes de validar o persistir key;
- no abre transaccion de escritura;
- no crea idempotencias;
- no crea movimientos;
- no toca `retiro_caja`.

## Pruebas ejecutadas

HTTP con flag apagado:

- sin token: `401`;
- token sin `Caja.Retirar`: `403`;
- token con `Caja.Retirar`, sin key: `503`;
- token con `Caja.Retirar`, key invalida: `503`;
- token con `Caja.Retirar`, key valida: `503`.

Pruebas puras:

- hash de retiro longitud `32`;
- hash estable con normalizacion equivalente;
- cambio de monto produce hash distinto;
- cambio de motivo produce hash distinto;
- cambio de referencia produce hash distinto;
- cambio de usuario produce hash distinto;
- GUID valido aceptado;
- GUID invalido rechazado;
- retiro igual al disponible valido;
- retiro superior al disponible invalido;
- retiro cero o negativo invalido;
- turno no abierto invalido.

## Integridad

Antes y despues:

- `caja_turno=1`;
- `movimiento_caja=3`;
- `caja_idempotencia=2`;
- `retiros_api=0`;
- `cierres_api=0`;
- `idempotencias RetiroCaja=0`;
- `ingreso_caja=9`;
- `retiro_caja=6`;
- `cierre_caja=15`;
- `ventas=1948`;
- `venta_pago=10`;
- inventario agregado `3296.00`;
- saldo agregado clientes `-2957962.50`.

## Limitaciones

- No existe transaccion real de retiro todavia.
- No existe idempotencia persistida para retiro todavia.
- No existe validacion transaccional de efectivo disponible todavia.
- No existe retiro API real.
- No existe reversa API.
- No existe cierre API real.
- WPF no esta conectado a Caja API.

## Recomendacion

Continuar con Fase 4F.15: implementar la transaccion interna real de `RetiroCaja` API manteniendo `EnableCajaApiWrite=false`, con pruebas no destructivas y sin ejecutar el primer retiro real todavia.
