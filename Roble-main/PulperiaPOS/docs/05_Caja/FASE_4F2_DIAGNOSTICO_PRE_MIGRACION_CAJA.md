# Fase 4F.2 - Diagnostico pre migracion de caja

Fecha/hora UTC: 2026-06-28 11:46:34 UTC.

## Alcance

Se ejecuto unicamente el diagnostico de solo lectura `database/diagnostics/008_DiagnosticoCajaTurnosYMovimientos.sql` y SELECTs agregados complementarios. No se ejecuto `database/migrations/008_CajaTurnosYMovimientos.sql`, no se ejecuto rollback y no se aplicaron cambios de datos ni de esquema.

## Confirmacion de ambiente

- Marca `Environment=Test`: confirmada.
- `caja_turno`: no existe.
- `movimiento_caja`: no existe.
- `UseVentasApiWrite=false`: confirmado.
- `EnableVentasApiWrite=false`: confirmado.

## Validacion del diagnostico 008

El script fue revisado antes de ejecutarse. Contiene `SELECT`, metadata de `sys.*`, agregaciones e inspeccion condicional de tablas futuras si existieran. No se detectaron operaciones de escritura como `INSERT`, `UPDATE`, `DELETE`, `MERGE`, `TRUNCATE`, `DROP`, `ALTER` ni `CREATE` fuera de comentarios.

Resultado de ejecucion:

- Result sets devueltos: 7.
- No se crearon objetos.
- No se modificaron registros.

## Estructura historica detectada

### `ingreso_caja`

| Columna | Tipo | Nullable | Observacion |
| --- | --- | --- | --- |
| `idIngreso` | `int` | No | PK historica. |
| `monto` | `decimal(10,2)` | No | Monto monetario. |
| `motivo` | `nvarchar(255)` | No | Texto libre. |
| `fecha` | `date` | No | Fecha local del cliente WPF. |
| `hora` | `time(7)` | No | Hora local del cliente WPF. |
| `usuario` | `nvarchar(100)` | No | Usuario como texto, sin FK. |

### `retiro_caja`

| Columna | Tipo | Nullable | Observacion |
| --- | --- | --- | --- |
| `idRetiro` | `int` | No | PK historica. |
| `monto` | `decimal(10,2)` | Si | Monto monetario. |
| `motivo` | `nvarchar(255)` | Si | Texto libre. |
| `fecha` | `date` | Si | Fecha local del cliente WPF. |
| `hora` | `time(7)` | Si | Hora local del cliente WPF. |

No existe columna de usuario responsable en la tabla.

### `cierre_caja`

| Columna | Tipo | Nullable | Observacion |
| --- | --- | --- | --- |
| `idCierre` | `int` | No | PK historica. |
| `fecha` | `date` | Si | Fecha local del cliente WPF. |
| `total_efectivo` | `decimal(10,2)` | Si | Total agregado historico. |
| `total_sinpe` | `decimal(10,2)` | Si | Total agregado historico. |
| `total_datafono` | `decimal(10,2)` | Si | Total agregado historico. |
| `observaciones` | `nvarchar(255)` | Si | Texto libre. |
| `hora` | `nvarchar(10)` | Si | Hora como texto. |

No existe usuario, caja logica, turno, efectivo contado, esperado ni diferencia formal.

## Integridad agregada

| Indicador | Resultado |
| --- | ---: |
| ingresos historicos | 9 |
| retiros historicos | 6 |
| cierres historicos | 15 |
| ventas historicas | 1925 |
| pagos API registrados | 10 |
| auditorias API registradas | 10 |
| idempotencias API registradas | 10 |
| ingresos sin usuario | 0 |
| ingresos con monto no positivo | 0 |
| ingresos con fecha nula | 0 |
| ingresos con hora nula | 0 |
| retiros sin columna de usuario | 6 |
| retiros con monto nulo | 0 |
| retiros con monto no positivo | 0 |
| retiros con fecha nula | 0 |
| retiros con hora nula | 0 |
| cierres sin columna de usuario | 15 |
| cierres con fecha nula | 0 |
| cierres con hora nula | 0 |
| cierres con efectivo nulo | 0 |
| cierres con efectivo negativo | 1 |
| posibles duplicados tecnicos en ingresos | 0 |
| posibles duplicados tecnicos en retiros | 0 |
| posibles duplicados tecnicos en cierres | 0 |

El cierre con efectivo negativo se clasifica como dato historico a revisar funcionalmente en reportes, pero no bloquea la migracion 008 porque la migracion no hace backfill ni interpreta datos historicos.

## Dependencias encontradas

- No se detectaron FKs existentes desde o hacia `ingreso_caja`, `retiro_caja` o `cierre_caja`.
- No se detectaron dependencias SQL server-side registradas en `sys.sql_expression_dependencies` para las tablas de caja revisadas.
- Dependencias WPF encontradas por codigo:
  - `IngresoCajaPage.xaml.cs`: inserta y consulta `ingreso_caja`.
  - `RetirosCajaPage.xaml.cs`: inserta y consulta `retiro_caja`.
  - `CierreCajaPage.xaml.cs`: calcula e inserta `cierre_caja`.
  - `CajaHelper.cs`: calcula caja desde `ventas`, `ingreso_caja`, `retiro_caja`, `cierre_caja` y `cliente`.

## Compatibilidad con `caja_turno`

- Puede iniciar vacia sin relacionarse a cierre historico.
- No existe caja fisica/logica previa en datos, por lo que se requiere una decision funcional para el `caja_codigo` inicial.
- Es viable iniciar con una unica caja logica de prueba en Test.
- El indice unico filtrado `UX_caja_turno_un_abierto_por_caja` impide mas de un turno `Abierto` o `EnCierre` por caja logica.
- La migracion usa UTC con `SYSUTCDATETIME()` para apertura, cierre y auditoria tecnica.
- El riesgo principal de transicion es que el sistema historico sigue usando fecha/hora local hasta migrar flujos a API.

## Compatibilidad con `movimiento_caja`

- Las tablas historicas tienen identificadores compatibles para referencias futuras:
  - `ventas.factura`.
  - `venta_pago.idPago`.
  - `ingreso_caja.idIngreso`.
  - `retiro_caja.idRetiro`.
- Las referencias a factura, pago, ingreso, retiro y reversa son nullable donde corresponde.
- `monto` usa `decimal(18,2)` y bloquea montos no positivos.
- No se detectaron conflictos de nombres de constraints o indices con la migracion 008.
- No se detecto uso de `float` ni `real` para dinero en las tablas nuevas.
- No se registran movimientos para Tarjeta, SINPE ni Saldo Cliente como efectivo fisico.

## Revision de migracion 008

Resultado: aprobada tecnicamente para aplicarse en Test.

Confirmaciones:

- Es aditiva.
- Es idempotente por `OBJECT_ID` y validaciones de indices.
- No altera tablas historicas.
- No hace backfill.
- No crea movimientos historicos.
- No modifica ventas, pagos, ingresos, retiros, cierres ni Cliente General.
- Crea PKs, FKs, CHECKs, defaults e indices.
- Permite tablas nuevas vacias.
- No activa ventas API.
- No activa caja API.

Hallazgo no bloqueante:

- Requiere decision funcional posterior para el valor inicial de `caja_codigo` antes de abrir el primer turno.

## Revision de rollback 008

Resultado: aprobado como rollback preparado, no ejecutado.

Confirmaciones:

- No se ejecuta automaticamente.
- Se bloquea si existen movimientos en `movimiento_caja`.
- Se bloquea si existen turnos en `caja_turno`.
- No toca tablas historicas.
- No borra ventas, pagos, ingresos, retiros ni cierres.
- Requiere revision manual antes de aplicarse.

## Clasificacion de hallazgos

| Hallazgo | Clasificacion | Decision |
| --- | --- | --- |
| Diagnostico 008 solo lectura | Aprobado sin ajustes | Ejecutado. |
| Tablas nuevas no existen | Aprobado sin ajustes | Compatible con migracion aditiva. |
| Retiros sin usuario en tabla historica | Requiere decision funcional | No bloquea porque no hay backfill. |
| Cierres sin usuario/turno | Requiere decision funcional | No bloquea porque no hay backfill. |
| Un cierre historico con efectivo negativo | Requiere decision funcional | Revisar en reportes, no bloquea migracion. |
| Caja logica inicial no definida | Requiere decision funcional | Definir antes de abrir turnos. |
| Sin conflictos de nombres | Aprobado sin ajustes | Compatible. |
| Rollback bloqueante si hay datos nuevos | Aprobado sin ajustes | Correcto para seguridad. |

## Decision final

Aprobado para aplicar la migracion 008 en base Test en la siguiente fase, sin backfill, sin activar ventas API y sin conectar WPF todavia.

## Siguiente fase exacta

Fase 4F.3: aplicar `database/migrations/008_CajaTurnosYMovimientos.sql` exclusivamente en base Test, ejecutar `database/diagnostics/008_ValidacionPostMigracionCaja.sql`, confirmar que `caja_turno` y `movimiento_caja` existen vacias, validar constraints/indices/FKs, mantener feature flags apagados y no crear turnos ni movimientos todavia.
