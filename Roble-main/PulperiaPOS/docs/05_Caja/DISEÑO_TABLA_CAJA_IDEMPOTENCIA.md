# Diseno tabla caja_idempotencia

## Tabla propuesta

```text
dbo.caja_idempotencia
```

Campos principales:

- `idCajaIdempotencia` bigint identity;
- `usuario_id` int;
- `idTurno` bigint nullable;
- `caja_codigo` nvarchar(50) nullable;
- `operacion` nvarchar(40);
- `idempotency_key` uniqueidentifier;
- `request_hash` varbinary(32);
- `estado` nvarchar(20);
- `idMovimiento` bigint nullable;
- `cierre_referencia_id` bigint nullable;
- `resultado_codigo` nvarchar(80) nullable;
- `creado_utc`, `actualizado_utc`, `completado_utc`;
- `metadata_minima` nvarchar(250) nullable;
- `row_version`.

## Operaciones

- `IngresoCaja`
- `RetiroCaja`
- `CerrarTurno`
- `AjusteCaja`
- `ReversaMovimiento`

Apertura de turno no se incluye por ahora. La apertura de Fase 4F.6 fue una prueba unica controlada, no un patron productivo.

## Estados

- `EnProceso`: operacion registrada y aun no finalizada.
- `Completada`: operacion finalizo y tiene referencia final.
- `Fallida`: intento fallido que puede requerir politica explicita de reintento.

## Constraints

- PK en `idCajaIdempotencia`.
- FK a `usuario`.
- FK nullable a `caja_turno`.
- FK nullable a `movimiento_caja`.
- CHECK de operaciones permitidas.
- CHECK de estados permitidos.
- CHECK de longitud hash `DATALENGTH(request_hash)=32`.
- CHECK de fecha completada posterior o igual a creada.
- CHECK de referencia requerida si estado es `Completada`.

## Indices

- Unico: `(usuario_id, operacion, idempotency_key)`.
- Busqueda de estados: `(estado, actualizado_utc)`.
- Busqueda por turno: `(idTurno, operacion, estado)`.
- Busqueda por movimiento: `(idMovimiento)` filtrado por `idMovimiento IS NOT NULL`.
- Busqueda por caja: `(caja_codigo, operacion, estado)`.

No se usan indices filtrados con `OR`.

## Relaciones evitadas

No se toca `venta_idempotencia` ni tablas historicas. El cierre futuro puede requerir una FK adicional cuando exista el modelo de cierre API.
