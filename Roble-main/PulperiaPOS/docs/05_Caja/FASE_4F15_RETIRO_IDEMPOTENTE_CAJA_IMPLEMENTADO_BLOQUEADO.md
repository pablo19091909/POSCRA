# Fase 4F.15 - RetiroCaja idempotente implementado y bloqueado

Fecha UTC: 2026-06-29 16:17:55 UTC

## Alcance

Se implemento en codigo la transaccion interna real para `POST /api/caja/retiros`, manteniendo toda escritura de Caja API bloqueada por `EnableCajaApiWrite=false`.

No se crearon retiros reales, movimientos `RetiroCaja`, idempotencias nuevas, cierres, ajustes, reversas ni cambios en tablas historicas.

## Componentes revisados

- `caja_turno`
- `movimiento_caja`
- `caja_idempotencia`
- `CajaRepository`
- `CajaService`
- `ICajaRepository`
- `ICajaService`
- `CajaController`
- `CajaIdempotencyService`
- Flujo idempotente existente de `IngresoCaja`

## Implementacion realizada

Se agrego `ICajaRepository.RegistrarRetiroAsync` y su implementacion en `CajaRepository`.

La operacion usa:

- una sola `SqlConnection`;
- una sola `SqlTransaction`;
- aislamiento `Serializable`;
- locks `UPDLOCK, HOLDLOCK` para idempotencia, turno y calculo de efectivo;
- parametros SQL;
- `CancellationToken`;
- manejo de errores SQL `2601` y `2627`.

## Flujo implementado

```text
Validar flag y Environment
-> validar request
-> validar Idempotency-Key
-> calcular hash RetiroCaja
-> abrir transaccion Serializable
-> validar usuario activo
-> bloquear idempotencia por usuario + RetiroCaja + key
-> resolver repeticion, conflicto o EnProceso
-> bloquear turno Abierto de caja
-> calcular efectivo disponible bajo lock
-> rechazar monto superior al disponible
-> crear caja_idempotencia EnProceso
-> crear movimiento_caja RetiroCaja
-> completar caja_idempotencia
-> devolver movimiento seguro
-> commit
```

## Seguridad por flag

Con `EnableCajaApiWrite=false`, el servicio responde `503` antes de validar key, calcular hash o abrir transaccion. Por eso la implementacion queda lista, pero no ejecutable todavia.

## Pruebas ejecutadas

HTTP con flag apagado:

- sin token: `401`;
- token sin `Caja.Retirar`: `403`;
- token con permiso, sin key: `503`;
- token con permiso, key invalida: `503`;
- token con permiso, key valida: `503`.

Pruebas puras:

- hash determinista de retiro;
- cambio de monto, motivo, referencia o usuario cambia hash;
- retiro igual al disponible valido;
- retiro menor al disponible valido;
- retiro superior al disponible invalido;
- retiro cero o negativo invalido;
- turno no abierto invalido;
- reglas de idempotencia repetida, hash distinto y EnProceso documentadas.

## Integridad

Antes y despues:

- `caja_turno=1`;
- `movimiento_caja=3`;
- `caja_idempotencia=2`;
- `retiros_api=0`;
- `idempotencias RetiroCaja=0`;
- `ingreso_caja=9`;
- `retiro_caja=6`;
- `cierre_caja=15`;
- efectivo esperado `1501.00`.

## Limitaciones

- No se ejecuto un retiro real.
- No se probo concurrencia real con escrituras.
- No se activo `EnableCajaApiWrite`.
- WPF no esta conectado a Caja API.
- Reversa y cierre real siguen fuera de alcance.

## Recomendacion

Continuar con Fase 4F.16: prueba controlada de primer retiro idempotente en Test, con `EnableCajaApiWrite=true` solo de forma temporal y operativa.
