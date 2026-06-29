# Diseno CajaTurno

## Objetivo

`caja_turno` representa una apertura operacional de caja por caja logica o punto de venta. Reemplaza progresivamente el cierre por fecha de calendario por un cierre por estado y periodo controlado.

## Campos propuestos

| Campo | Tipo propuesto | Proposito |
| --- | --- | --- |
| `idTurno` | `BIGINT IDENTITY` | Identificador del turno. |
| `caja_codigo` | `NVARCHAR(50)` | Caja o punto de venta logico. |
| `estado` | `NVARCHAR(20)` | `Abierto`, `EnCierre`, `Cerrado`, `Anulado`. |
| `usuario_apertura_id` | `INT` | FK a `usuario`. |
| `usuario_cierre_id` | `INT NULL` | FK a `usuario`. |
| `apertura_utc` | `DATETIME2(0)` | Hora servidor UTC de apertura. |
| `cierre_utc` | `DATETIME2(0) NULL` | Hora servidor UTC de cierre. |
| `fondo_inicial` | `DECIMAL(18,2)` | Efectivo inicial. |
| `efectivo_esperado` | `DECIMAL(18,2) NULL` | Calculado al cierre. |
| `efectivo_contado` | `DECIMAL(18,2) NULL` | Capturado por operador. |
| `diferencia` | `DECIMAL(18,2) NULL` | Contado - esperado. |
| `observacion_apertura` | `NVARCHAR(250) NULL` | Nota de apertura. |
| `observacion_cierre` | `NVARCHAR(250) NULL` | Nota de cierre. |
| `cierre_caja_id` | `INT NULL` | Referencia opcional a cierre historico durante transicion. |
| `row_version` | `ROWVERSION` | Concurrencia optimista. |

## Reglas

- Unica caja abierta o en cierre por `caja_codigo`.
- La venta API debe rechazar escritura si no hay turno abierto.
- Una venta no debe entrar si el turno esta `EnCierre`, `Cerrado` o `Anulado`.
- El cierre debe cambiar estado a `EnCierre`, calcular esperado, capturar contado y finalizar en `Cerrado`.
- La anulacion de turno solo aplica si no hay movimientos confirmados o si existe procedimiento administrativo posterior.

## Concurrencia

- Indice unico filtrado por `caja_codigo` cuando `estado IN ('Abierto','EnCierre')`.
- Transiciones de estado dentro de transaccion.
- `row_version` para evitar cierres simultaneos desde dos clientes.

## Comportamiento ante errores

- Sin turno abierto: venta API responde error seguro y no escribe.
- Turno cerrado: venta API responde error seguro y no escribe.
- Turno de otra caja: venta API responde error seguro.
- Cierre concurrente: venta y cierre compiten por estado; uno debe ganar transaccionalmente y el otro fallar sin escritura parcial.
