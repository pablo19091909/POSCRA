# Diseno MovimientoCaja

## Objetivo

`movimiento_caja` sera la fuente de verdad inmutable para efectivo fisico. Cada aumento o disminucion de efectivo confirmado se registra como movimiento; las correcciones se hacen con reversos, no con updates ni deletes.

## Campos propuestos

| Campo | Tipo propuesto | Proposito |
| --- | --- | --- |
| `idMovimiento` | `BIGINT IDENTITY` | Identificador. |
| `idTurno` | `BIGINT` | FK a `caja_turno`. |
| `tipo_movimiento` | `NVARCHAR(30)` | Tipo financiero. |
| `origen` | `NVARCHAR(30)` | `POS.Api`, `WPF`, `MigracionFutura`, `AjusteManual`. |
| `monto` | `DECIMAL(18,2)` | Monto positivo. |
| `moneda` | `CHAR(3)` | Inicialmente `CRC`. |
| `fecha_hora_utc` | `DATETIME2(0)` | Hora servidor UTC. |
| `usuario_id` | `INT` | Usuario responsable. |
| `factura` | `INT NULL` | Venta asociada. |
| `pago_id` | `BIGINT NULL` | Pago asociado. |
| `ingreso_caja_id` | `INT NULL` | Compatibilidad con ingreso historico/futuro. |
| `retiro_caja_id` | `INT NULL` | Compatibilidad con retiro historico/futuro. |
| `referencia` | `NVARCHAR(100) NULL` | Referencia operativa. |
| `observacion` | `NVARCHAR(250) NULL` | Nota segura. |
| `estado` | `NVARCHAR(20)` | `Confirmado` o `Reversado`. |
| `reversa_de_movimiento_id` | `BIGINT NULL` | Movimiento original si es reversa. |
| `correlacion_tecnica` | `UNIQUEIDENTIFIER NULL` | Correlacion/idempotencia sin exponer secretos. |

## Tipos evaluados

- `FondoInicial`
- `VentaEfectivo`
- `IngresoCaja`
- `RetiroCaja`
- `AjustePositivo`
- `AjusteNegativo`
- `DevolucionEfectivo`
- `CierreDiferencia`
- `Reversa`

## Reglas

- No actualizar ni borrar movimientos confirmados.
- Correccion por movimiento inverso.
- Monto siempre positivo; el tipo define efecto positivo o negativo.
- Tarjeta, SINPE y Saldo Cliente no crean movimiento de efectivo fisico.
- Venta API en efectivo crea un unico movimiento `VentaEfectivo` por total neto.
- No sumar `monto_recibido` ni restar `vuelto` como movimientos separados para la venta normal.
- Relacion futura con `venta_pago` mediante `pago_id`.

## Efecto por tipo

| Tipo | Efecto en efectivo esperado |
| --- | ---: |
| `FondoInicial` | Suma |
| `VentaEfectivo` | Suma |
| `IngresoCaja` | Suma |
| `RetiroCaja` | Resta |
| `AjustePositivo` | Suma |
| `AjusteNegativo` | Resta |
| `DevolucionEfectivo` | Resta |
| `CierreDiferencia` | Informativo/ajuste segun politica |
| `Reversa` | Invierte movimiento referenciado |
