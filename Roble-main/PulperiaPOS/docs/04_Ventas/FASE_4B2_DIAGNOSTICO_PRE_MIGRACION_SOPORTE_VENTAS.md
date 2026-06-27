# Fase 4B.2 - Diagnostico pre migracion de soporte para ventas

Fecha UTC: 2026-06-26 11:02:57 UTC

## Alcance

Se ejecuto unicamente el diagnostico de lectura `database/diagnostics/007_DiagnosticoSoporteVentasTransaccionales.sql` y consultas SELECT complementarias de metadatos/agregados.

No se ejecuto la migracion `007_SoporteVentasTransaccionales.sql`. No se ejecuto rollback. No se ejecutaron `INSERT`, `UPDATE`, `DELETE`, `ALTER`, `CREATE`, `DROP`, `MERGE`, `TRUNCATE` ni migraciones.

## Validacion del script de diagnostico

Resultado: aprobado.

El script contiene consultas de lectura, metadatos, `INFORMATION_SCHEMA`, `sys.columns`, `sys.indexes`, `sys.foreign_keys`, agregaciones y validaciones de integridad. No se detectaron operaciones de escritura ni cambios de esquema.

Al ejecutarse produjo 24 result sets de metadatos/agregados.

## Estructura real de ventas

Tabla: `ventas`.

- PK real: constraint de primary key sobre `factura`.
- Columna PK: `factura`.
- Tipo: `int`.
- Nulabilidad: no nullable.
- Unicidad: unica por PK.
- Facturas nulas: 0.
- Facturas duplicadas: 0.
- Indice unico sobre factura: existe por la PK.

Decision: `ventas.factura` puede usarse de forma segura como relacion futura para `venta_pago`, `venta_idempotencia` y `venta_auditoria`.

## Estructura real de DetalleVenta

Tabla: `DetalleVenta`.

- PK real: `idDetalle`.
- Tipo de PK: `int`, no nullable.
- FK real hacia venta: `DetalleVenta.factura -> ventas.factura`.
- Campo real usado para relacionar detalle y venta: `factura`.
- `cantidad`: `int`, no nullable.
- `precio_unitario`: `decimal(10,2)`, no nullable.

## Integridad agregada

- Total ventas: 1881.
- Ventas sin detalle: 0.
- Detalles sin venta: 0.
- Ventas sin cliente: 0.
- Ventas sin usuario: 0.
- Ventas sin metodo de pago: 0.
- Ventas sin fecha/hora: 0.
- Ventas con total nulo, cero o negativo: 0.
- Ventas con detalle y total inconsistente: 0.

## Pagos actuales

- Metodos de pago distintos guardados actualmente: 5.
- Efectivo inconsistente segun columnas existentes: 0.
- Tarjeta sin voucher: 0.
- SINPE sin comprobante: 0.
- Ventas en dolares detectadas: 0.
- Dolares sin tipo de cambio guardado en venta: 0.

Observacion: el flujo historico incluye al menos un metodo no contemplado en el CHECK futuro de `venta_pago` para ventas normales, por ejemplo el flujo de donaciones. Como la migracion no hace backfill historico, esto no bloquea la creacion de tablas vacias, pero si requiere decision antes de migrar donaciones o poblar pagos historicos.

## Dependencias encontradas

### Base de datos

Foreign keys relevantes:

- `ventas.cliente_id -> cliente.idCliente`.
- `ventas.usuario_id -> usuario.idUsuario`.
- `DetalleVenta.factura -> ventas.factura`.
- `DetalleVenta.producto_id -> inventario.idProducto`.

Dependencias SQL por vistas, triggers, procedimientos o funciones detectadas mediante `sys.sql_expression_dependencies`: 0.

### WPF

Dependencias identificables por codigo:

- `VentasPage.xaml.cs`: crea ventas, detalle, stock, saldo, recibo y pagos actuales.
- `VentasCrudWindow.xaml.cs`: carga, agrega, actualiza, elimina y reimprime ventas.
- `DetalleVentaWindow.xaml.cs`: consulta detalle de venta.
- `CajaHelper.cs`: calcula caja desde ventas en efectivo.
- `CierreCajaPage.xaml.cs`: calcula totales por metodo de pago.
- `ClientePage.xaml.cs`: bloquea eliminacion de clientes con ventas.
- `DonacionesPage.xaml.cs`: crea registros en `ventas` y `DetalleVenta` para donaciones.
- `RawPrinterHelper.cs`: imprime metodo de pago, vuelto y comprobante.

## Validacion del script 007

Resultado: aprobado con observacion.

Validaciones:

- Es aditivo.
- Es idempotente mediante `OBJECT_ID` e indices condicionales.
- No modifica ni elimina datos historicos.
- No altera destructivamente `ventas`, `DetalleVenta`, `inventario` ni `cliente`.
- No cambia stock, saldo, caja ni cierres.
- Usa `ventas.factura`, validada como PK unica no nullable.
- Usa `decimal` para dinero en `venta_pago`.
- Usa `datetime2` y `SYSUTCDATETIME()` para fechas UTC.
- Crea FK, indices y CHECK constraints sobre tablas nuevas vacias.
- No hay conflictos de nombres con tablas, constraints o indices existentes.
- Permite que las nuevas tablas esten vacias al aplicar migracion.
- No activa funcionalidades automaticamente.

Observacion:

- `venta_pago.CK_venta_pago_metodo` contempla metodos de venta normal (`Efectivo`, `Tarjeta`, `Sinpe`, `Dolares`, `SaldoCliente`). No contempla donaciones. Esto no bloquea la migracion porque no hay backfill ni endpoint activo, pero debe resolverse antes de migrar `DonacionesPage` o pagos historicos.

## Validacion del rollback

Resultado: aprobado.

- Falla de manera segura si `venta_pago` contiene registros.
- Falla de manera segura si `venta_auditoria` contiene registros.
- Falla de manera segura si `venta_idempotencia` contiene registros.
- No toca `ventas`, `DetalleVenta`, `inventario`, `cliente`, caja ni historicos.
- Solo elimina tablas de soporte vacias.

## Clasificacion de hallazgos

| Hallazgo | Clasificacion |
|---|---|
| `factura` es PK unica, no nullable y sin duplicados | Aprobado sin ajustes |
| `DetalleVenta.factura` tiene FK real a `ventas.factura` | Aprobado sin ajustes |
| Ventas sin detalle = 0 | Aprobado sin ajustes |
| Detalles sin venta = 0 | Aprobado sin ajustes |
| Totales inconsistentes = 0 | Aprobado sin ajustes |
| Ventas sin usuario/cliente/metodo/fecha = 0 | Aprobado sin ajustes |
| Tablas de soporte no existen aun | Aprobado sin ajustes |
| Sin conflictos de nombres para tablas/indices/constraints 007 | Aprobado sin ajustes |
| Metodo historico de donacion no cubierto por `venta_pago` | Requiere decision antes de migrar donaciones o backfill historico |

## Recomendacion final

Aprobado con observacion.

La migracion 007 puede aplicarse en una fase posterior porque es aditiva, crea tablas vacias, no toca historicos y usa `ventas.factura` como FK segura.

Antes de migrar donaciones o pagos historicos, decidir si:

1. `Donacion` sera un metodo valido en `venta_pago`; o
2. donaciones tendran endpoint/modelo separado; o
3. no se hara backfill historico hacia `venta_pago`.

## Siguiente fase exacta

Fase 4B.3:

1. Confirmar respaldo reciente.
2. Ejecutar manualmente `database/migrations/007_SoporteVentasTransaccionales.sql`.
3. Ejecutar `database/diagnostics/007_ValidacionPostMigracionSoporteVentas.sql`.
4. Confirmar que `venta_idempotencia`, `venta_pago` y `venta_auditoria` existen y estan vacias.
5. Confirmar que `ventas`, `DetalleVenta`, `inventario`, `cliente`, caja y cierres no cambiaron.
