# Fase 4F.18 - Diseno cierre turno Caja API bloqueado

Fecha UTC: 2026-06-29 21:12:41 UTC

## Alcance

Se ejecuto auditoria, diseno tecnico y preparacion no ejecutada del cierre de turno de Caja API. No se activo `EnableCajaApiWrite`, no se cerro el turno Test y no se escribio en `caja_turno`, `movimiento_caja`, `caja_idempotencia` ni tablas historicas.

## Auditoria del cierre historico

El cierre historico actual vive en WPF:

- `PulperiaPOS/Views/CierreCajaPage.xaml.cs`.
- `PulperiaPOS/CajaHelper.cs`.
- Tabla `cierre_caja`.

Comportamiento actual:

- Calcula efectivo con `CajaHelper.ObtenerTotalesCaja()`.
- Usa fecha y hora local con `DateTime.Now`.
- Lee `ventas`, `ingreso_caja`, `retiro_caja`, `cliente` y `cierre_caja`.
- Inserta directamente en `cierre_caja` los campos `fecha`, `hora`, `total_efectivo`, `total_sinpe`, `total_datafono` y `observaciones`.
- No registra efectivo contado separado del esperado.
- No registra diferencia calculada.
- No usa turno, `row_version`, idempotencia ni transaccion de cierre.
- No impide doble cierre por concurrencia.
- Usa `double` en `CajaHelper` para totales, aunque WPF convierte algunos valores a `decimal` al insertar.

Riesgos detectados:

- cierre por fecha calendario y ultimo cierre del dia, sensible a hora local;
- posible doble cierre si dos operadores ejecutan la pantalla;
- sin concurrencia optimista;
- sin idempotencia;
- sin aislamiento por turno;
- historicos mezclados con ventas y movimientos manuales;
- no hay modelo explicito de faltante/sobrante.

Caja API no debe reutilizar directamente `cierre_caja` porque `cierre_caja` es un historico diario WPF, no un cierre transaccional por turno. El cierre por turno es mas seguro porque ata el efectivo esperado a un conjunto cerrado de movimientos, usa UTC, permite `row_version`, permite idempotencia y evita recalculos por fecha local.

## Modelo real validado

Columnas reales relevantes:

- `caja_turno`: `estado`, `usuario_cierre_id`, `apertura_utc`, `cierre_utc`, `fondo_inicial`, `efectivo_esperado`, `efectivo_contado`, `diferencia`, `observacion_cierre`, `cierre_caja_id`, `row_version`.
- `movimiento_caja`: `tipo_movimiento`, `origen`, `monto`, `moneda`, `fecha_hora_utc`, `usuario_id`, `factura`, `pago_id`, `ingreso_caja_id`, `retiro_caja_id`, `referencia`, `observacion`, `estado`, `reversa_de_movimiento_id`.
- `caja_idempotencia`: `usuario_id`, `idTurno`, `caja_codigo`, `operacion`, `idempotency_key`, `request_hash`, `estado`, `idMovimiento`, `cierre_referencia_id`, `resultado_codigo`, `metadata_minima`, `row_version`.
- `cierre_caja`: `idCierre`, `fecha`, `total_efectivo`, `total_sinpe`, `total_datafono`, `observaciones`, `hora`.

Constraints relevantes:

- turno: `Abierto`, `EnCierre`, `Cerrado`, `Anulado`;
- movimiento: `FondoInicial`, `VentaEfectivo`, `IngresoCaja`, `RetiroCaja`, `AjustePositivo`, `AjusteNegativo`, `DevolucionEfectivo`, `CierreDiferencia`, `Reversa`;
- origen: `POS.Api`, `WPF`, `MigracionFutura`, `AjusteManual`;
- moneda: `CRC`, `USD`;
- movimiento `monto > 0`;
- idempotencia admite `CerrarTurno`.

## Estado actual del endpoint

Existe `POST /api/caja/turnos/{idTurno}/cerrar` con permiso `Caja.Cerrar`, pero esta bloqueado por `EnableCajaApiWrite=false`. Con flag apagado devuelve `503` antes de validar cuerpo o `Idempotency-Key`.

Pruebas no destructivas ejecutadas:

- sin token: `401`;
- token sin `Caja.Cerrar`: `403`;
- token autorizado sin key y flag apagado: `503`;
- token autorizado con key invalida y flag apagado: `503`;
- token autorizado con key valida y flag apagado: `503`.

## Contrato futuro

Request permitido:

```json
{
  "efectivoContado": 201.00,
  "observacion": "texto opcional o requerido segun diferencia",
  "rowVersion": "base64"
}
```

Header obligatorio cuando escritura este habilitada:

```text
Idempotency-Key: <GUID>
```

El cliente no puede enviar como autoridad:

- efectivo esperado;
- diferencia;
- usuario;
- fecha UTC;
- estado;
- caja alternativa;
- movimientos;
- monto de cierre;
- referencias historicas;
- valores calculados localmente.

Respuesta segura futura:

- estado final;
- efectivo esperado calculado por servidor;
- efectivo contado recibido;
- diferencia calculada por servidor;
- fecha UTC de cierre;
- resumen seguro de movimientos;
- indicador de `CierreDiferencia` creado.

No debe incluir key, hash, SQL, stack traces, tokens, IDs internos no necesarios ni secretos.

## Regla financiera

```text
EfectivoEsperado = sumatoria neta de movimientos confirmados del turno
Diferencia = EfectivoContado - EfectivoEsperado
```

Para el turno Test actual:

```text
1000.00 + 501.00 - 1300.00 = 201.00
```

`CierreDiferencia` debe crearse solo si `Diferencia != 0`. Como `movimiento_caja.monto` exige `monto > 0`, el monto del movimiento debe ser el valor absoluto de la diferencia. La direccion financiera se conserva en `caja_turno.diferencia`: positiva para sobrante y negativa para faltante. El calculo de efectivo esperado previo al cierre no debe modificarse retrospectivamente.

## Integridad antes/despues

Sin cambios durante esta fase:

- `caja_turno=1`.
- `movimiento_caja=5`.
- `caja_idempotencia=4`.
- `ingreso_caja=9`.
- `retiro_caja=6`.
- `cierre_caja=15`.
- `EfectivoEsperado=201.00`.
- `row_version` del turno sin cambios.

## Resultado

La fase queda bloqueada correctamente: el diseno esta preparado, el endpoint permanece inaccesible para escritura y no hubo cambios de datos.

## Recomendacion

Continuar con Fase 4F.19: implementacion controlada del cierre de turno API manteniendo `EnableCajaApiWrite=false` por defecto, con endpoint bloqueado hasta autorizacion explicita de prueba sintetica.
