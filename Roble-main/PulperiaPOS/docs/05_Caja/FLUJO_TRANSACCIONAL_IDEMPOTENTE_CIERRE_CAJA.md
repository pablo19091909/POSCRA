# Flujo transaccional idempotente cierre Caja API

Fecha UTC: 2026-06-29 21:12:41 UTC

## Flujo futuro

```text
Validar JWT y permiso Caja.Cerrar
-> validar flag EnableCajaApiWrite y Environment=Test
-> validar Idempotency-Key y request
-> calcular hash canonico CerrarTurno
-> abrir SqlConnection
-> iniciar SqlTransaction Serializable
-> bloquear idempotencia por usuario + operacion + key
-> resolver turno y validar rowVersion
-> validar estado Abierto
-> cambiar turno a EnCierre bajo bloqueo
-> calcular efectivo esperado desde movimiento_caja
-> calcular diferencia contra efectivo contado
-> validar observacion si diferencia != 0
-> crear CierreDiferencia solo si aplica
-> actualizar caja_turno a Cerrado con cierre completo
-> completar caja_idempotencia
-> devolver respuesta segura
-> commit
```

## Reglas criticas

- Una sola `SqlConnection`.
- Una sola `SqlTransaction`.
- Aislamiento `Serializable`.
- SQL parametrizado.
- `CancellationToken`.
- `decimal`, nunca `double` o `float`.
- No insertar en `cierre_caja`.
- No modificar `ingreso_caja` ni `retiro_caja`.
- No permitir `Cerrado` sin datos completos.
- No permitir idempotencia `Completada` sin cierre real.
- No permitir cierre real sin idempotencia `Completada`.
- No guardar request completo, token, key, hash, SQL ni datos sensibles en logs.

## Idempotencia

Operacion fija:

```text
CerrarTurno
```

Estados:

- `EnProceso`: devolver conflicto temporal.
- `Completada` con mismo hash: devolver resultado seguro equivalente.
- `Completada` con hash distinto: conflicto.
- `Fallida`: no reutilizar silenciosamente; definir politica de recuperacion futura.

`cierre_referencia_id` puede usarse como referencia de cierre logico futuro. Como no existe tabla especifica de cierre API, la referencia recomendada inicial es el `idTurno` ya cerrado, sin exponerlo al cliente como dato sensible.

## Rollback

Si falla antes del commit:

- no queda turno `Cerrado`;
- no queda `CierreDiferencia`;
- no queda idempotencia `Completada`;
- no se modifica historico.

Si se requiere recuperacion de `EnProceso`, debe ser una fase posterior con auditoria, no una autocorreccion silenciosa.

## Respuesta segura

Debe incluir:

- estado final;
- efectivo esperado;
- efectivo contado;
- diferencia;
- fecha UTC de cierre;
- resumen seguro por tipo de movimiento;
- `cierreDiferenciaCreado=true/false`.

No debe incluir:

- Idempotency-Key;
- request hash;
- JWT;
- SQL;
- stack trace;
- connection string;
- datos personales;
- detalles internos innecesarios.
