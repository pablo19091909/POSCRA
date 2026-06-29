# Reglas de apertura de turno Caja API

## Entrada permitida

`POST /api/caja/turnos/abrir` acepta solo:

- `cajaCodigo`;
- `fondoInicial`;
- `observacion`.

El request no puede enviar usuario, estado, fecha oficial, efectivo esperado, efectivo contado, diferencia, id de turno, id de movimiento ni datos de cierre.

## Validaciones

- `cajaCodigo` es requerido.
- `cajaCodigo` no puede superar 50 caracteres.
- `fondoInicial` debe ser mayor a `0` y caber en `decimal(18,2)`.
- `observacion` es opcional y no puede superar 250 caracteres.
- El usuario debe venir del JWT y existir activo en `usuario`.
- La base debe cumplir `Environment=Test` y `writes_allowed_for_testing=1`.
- `EnableCajaApiWrite` debe estar activo para permitir escritura.

## Escritura preparada

La apertura crea en una misma transaccion:

- un turno en `caja_turno` con estado `Abierto`;
- un movimiento en `movimiento_caja` con tipo `FondoInicial`;
- relacion por `idTurno`;
- fecha UTC generada por SQL Server;
- `row_version` generado por SQL Server.

## Fondo inicial

El fondo inicial se guarda en `caja_turno.fondo_inicial` y en el movimiento `FondoInicial`.

Para esta API, fondo `0` se rechaza porque el esquema real de `movimiento_caja` exige `monto > 0` y la operacion debe crear el movimiento inicial.

## Cierre no incluido

La apertura no crea ingresos, retiros, cierres, reversas, ventas, pagos ni auditoria de ventas.
