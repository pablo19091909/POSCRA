# Flujo transaccional cierre turno implementado

Fecha UTC: 2026-06-29 21:37:22 UTC

## Flujo implementado

```text
JWT + Caja.Cerrar
-> flag y ambiente
-> validacion request/key/rowVersion
-> hash canonico CerrarTurno
-> SqlConnection
-> SqlTransaction Serializable
-> validar usuario activo
-> bloquear idempotencia usuario + operacion + key
-> resolver idempotencia existente
-> bloquear turno
-> validar rowVersion
-> validar estado Abierto
-> crear idempotencia EnProceso
-> cambiar a EnCierre
-> calcular efectivo esperado desde movimiento_caja
-> calcular diferencia
-> validar observacion si diferencia != 0
-> crear CierreDiferencia si aplica
-> actualizar turno a Cerrado
-> completar idempotencia
-> leer resultado seguro
-> commit
```

## Calculo de efectivo esperado

Usa exclusivamente `movimiento_caja` dentro de la transaccion:

- suma `FondoInicial`, `VentaEfectivo`, `IngresoCaja`, `AjustePositivo`;
- resta `RetiroCaja`, `AjusteNegativo`, `DevolucionEfectivo`;
- contempla `Reversa` segun el movimiento revertido;
- excluye `CierreDiferencia`.

No usa fechas locales ni tablas historicas.

## Respuesta segura

Incluye:

- estado final;
- efectivo esperado;
- efectivo contado;
- diferencia;
- fecha UTC cierre;
- indicador de `CierreDiferencia`;
- resumen seguro de movimientos.

No incluye key, hash, SQL, token, connection string ni datos personales.

## Proteccion actual

Con `EnableCajaApiWrite=false`, el flujo transaccional no se ejecuta y el endpoint responde `503`.
