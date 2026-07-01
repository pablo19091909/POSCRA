# Contrato WPF - Cierre Caja API

Fecha UTC: 2026-06-30

## Endpoints revisados

- `GET /api/caja/turnos/abierto?cajaCodigo=CAJA_PRINCIPAL_TEST`
- `GET /api/caja/turnos/{id}/pre-cierre`
- `GET /api/caja/turnos/{id}/movimientos`
- `POST /api/caja/turnos/{id}/cerrar`

## Permisos

- Lectura: `Caja.Ver`.
- Cierre: `Caja.Cerrar`.

## Request de cierre

WPF debe enviar:

- `efectivoContado`: decimal.
- `observacion`: texto opcional, requerido visualmente si hay diferencia estimada.
- `rowVersion`: Base64 valido.
- `Idempotency-Key`: solo por header.

WPF no debe enviar:

- Usuario.
- Fecha.
- Esperado.
- Diferencia final.
- Estado.
- Movimiento.
- Hash.
- UTC.
- Identificador de cierre.

## Response de cierre

API retorna:

- `idTurno`.
- `cajaCodigo`.
- `estado`.
- `efectivoEsperado`.
- `efectivoContado`.
- `diferencia`.
- `cierreUtc`.
- `cierreDiferenciaCreado`.
- `resumen`.

## Reglas

- El servidor calcula efectivo esperado y diferencia final.
- El servidor decide si crea `CierreDiferencia`.
- El servidor controla `Abierto -> EnCierre -> Cerrado`.
- El servidor valida `rowVersion`.
- El servidor aplica idempotencia.
- Caja API no usa `cierre_caja` historico para persistir el cierre moderno.

## Codigos esperados

- `400`: solicitud invalida.
- `401`: sin token o token invalido.
- `403`: sin permiso.
- `404`: turno no encontrado.
- `409`: conflicto de estado, version o idempotencia.
- `503`: Caja API no disponible o escritura apagada.
