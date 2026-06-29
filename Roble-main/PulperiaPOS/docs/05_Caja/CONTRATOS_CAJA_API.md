# Contratos Caja API

## Feature flag

Toda escritura depende de:

```text
EnableCajaApiWrite=true
Environment=Test
writes_allowed_for_testing=1
```

Con el flag apagado, los endpoints de escritura devuelven 503 seguro.

## Requests

### `AbrirCajaTurnoRequest`

- `cajaCodigo`
- `fondoInicial`
- `observacion`

El cliente no envia usuario oficial, fecha oficial, estado, efectivo esperado ni diferencia.

### `RegistrarIngresoCajaRequest`

- `monto`
- `motivo`
- `referencia`

El cliente no envia turno, usuario, UTC, estado ni disponible.

### `RegistrarRetiroCajaRequest`

- `monto`
- `motivo`
- `referencia`

El disponible se calculara server-side.

### `CerrarCajaTurnoRequest`

- `efectivoContado`
- `observacion`
- `rowVersion`

El esperado y la diferencia se calculan server-side.

## Responses

- `CajaTurnoResponse`
- `MovimientoCajaResponse`
- `PreCierreCajaResponse`
- `CierreCajaResponse`
- `CajaErrorResponse`

Las respuestas no exponen SQL, servidor, base, connection strings, tokens ni datos sensibles.

## Endpoints

- `GET /api/caja/turnos/abierto`
- `POST /api/caja/turnos/abrir`
- `POST /api/caja/ingresos`
- `POST /api/caja/retiros`
- `GET /api/caja/turnos/{id}/pre-cierre`
- `POST /api/caja/turnos/{id}/cerrar`
- `GET /api/caja/turnos/{id}/movimientos`
