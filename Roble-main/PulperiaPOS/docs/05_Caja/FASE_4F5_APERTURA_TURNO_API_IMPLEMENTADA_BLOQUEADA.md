# Fase 4F.5 - Apertura de turno API implementada y bloqueada

Fecha/hora UTC: 2026-06-28.

## Resultado

Se preparo en POS.Api la transaccion interna real para apertura de turno de caja. La escritura permanece inaccesible porque `FeatureFlags:EnableCajaApiWrite=false`.

No se conecto WPF a Caja API y no se modificaron flujos historicos de caja.

## Modelo confirmado

- `caja_turno.idTurno`: PK real.
- `caja_turno.caja_codigo`: codigo logico de caja, `nvarchar(50)`.
- `caja_turno.estado`: permite `Abierto`, `EnCierre`, `Cerrado`, `Anulado`.
- `caja_turno.usuario_apertura_id`: usuario autenticado desde JWT.
- `caja_turno.apertura_utc`: fecha UTC generada por SQL Server.
- `caja_turno.fondo_inicial`: `decimal(18,2)`.
- Campos de cierre quedan `NULL` al abrir: usuario de cierre, cierre UTC, esperado, contado, diferencia, observacion de cierre y cierre historico.
- `caja_turno.row_version`: version optimista generada por SQL Server.
- `movimiento_caja.idMovimiento`: PK real.
- `movimiento_caja.idTurno`: FK al turno.
- Fondo inicial usa `tipo_movimiento='FondoInicial'`, `origen='POS.Api'`, `moneda='CRC'`, `estado='Confirmado'`.
- `movimiento_caja.monto` exige `> 0`.

## Componentes modificados

- `POS.Api/Application/Caja/ICajaRepository.cs`
- `POS.Api/Application/Caja/CajaService.cs`
- `POS.Api/Infrastructure/Data/Caja/CajaRepository.cs`

## Secuencia preparada

```text
Validar flag EnableCajaApiWrite
-> validar Environment=Test y writes_allowed_for_testing=1
-> validar request y usuario JWT
-> abrir SqlConnection
-> iniciar SqlTransaction Serializable
-> validar usuario activo
-> bloquear busqueda de turno Abierto/EnCierre por caja
-> insertar caja_turno Abierto
-> insertar movimiento_caja FondoInicial
-> leer turno creado con row_version
-> commit
```

Ante error, la transaccion ejecuta rollback y no debe quedar turno sin movimiento ni movimiento sin turno.

## Decisiones

- Fondo inicial `0` se rechaza en API porque la fase exige crear un movimiento `FondoInicial` y `movimiento_caja.monto` requiere monto positivo.
- El cliente no decide usuario, fecha, estado, efectivo esperado, contado, diferencia, identificadores ni movimiento inicial.
- La observacion es opcional y se limita a 250 caracteres.
- La caja logica prevista para pruebas futuras es `CAJA_PRINCIPAL_TEST`.

## Pruebas no destructivas

Con el flag apagado, `POST /api/caja/turnos/abrir` debe responder `503` seguro antes de abrir transaccion o insertar datos.

Las pruebas de seguridad autorizadas son:

- sin token: `401`;
- token sin `Caja.Abrir`: `403`;
- token con `Caja.Abrir` y flag apagado: `503`;
- health checks publicos: `200`.

## Integridad

La fase no debe crear registros en:

- `caja_turno`;
- `movimiento_caja`;
- `ingreso_caja`;
- `retiro_caja`;
- `cierre_caja`.

Ventas, inventario y saldos pueden cambiar por actividad externa concurrente y no se atribuyen a Caja API sin evidencia.

## Riesgos pendientes

- No se ha activado ni probado escritura real en Test.
- No existe idempotencia de caja.
- Ingresos, retiros, cierres y ventas efectivas aun no escriben `movimiento_caja`.
- WPF aun no consume Caja API.

## Recomendacion

Avanzar a Fase 4F.6 para preparar prueba controlada de apertura real en Test, activando el flag solo de forma local y temporal, con linea base inmediata y rollback operativo definido.
