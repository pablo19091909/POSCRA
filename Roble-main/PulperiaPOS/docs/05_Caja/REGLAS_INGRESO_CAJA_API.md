# Reglas de Ingreso Caja API

## Contrato

`POST /api/caja/ingresos` acepta:

- `cajaCodigo`;
- `monto`;
- `motivo`;
- `referencia`.

El cliente no envia `idTurno`, usuario, fecha UTC, tipo de movimiento, estado, efectivo esperado ni identificadores internos como autoridad.

## Validaciones

- `Caja.Ingresar` requerido.
- `cajaCodigo` requerido y maximo 50 caracteres.
- Debe existir un turno `Abierto` para la caja.
- No se permite turno `EnCierre`, `Cerrado` o inexistente.
- `monto` debe ser mayor que cero.
- `monto` debe caber en `decimal(18,2)`.
- `motivo` requerido y maximo 250 caracteres.
- `referencia` opcional y maximo 250 caracteres.
- Usuario debe venir del JWT y estar activo.
- Escritura requiere `EnableCajaApiWrite=true`.
- Ambiente debe ser `Test` con `writes_allowed_for_testing=1`.

## Secuencia transaccional futura

```text
Validar flag y ambiente
-> validar request y usuario JWT
-> abrir SqlConnection
-> iniciar SqlTransaction
-> validar usuario activo
-> buscar turno Abierto con bloqueo
-> insertar movimiento_caja tipo IngresoCaja
-> commit
```

## Modelo de datos

El ingreso API escribe solo en `movimiento_caja`.

No debe insertar en `ingreso_caja` historico. Los reportes futuros deben distinguir caja historica y Caja API.

## Correcciones futuras

Una correccion financiera no debe editar el movimiento original. Debe generar una reversa o ajuste auditado.
