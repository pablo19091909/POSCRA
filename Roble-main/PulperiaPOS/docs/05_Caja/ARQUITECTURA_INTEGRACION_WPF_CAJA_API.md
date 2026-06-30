# Arquitectura de integracion WPF Caja API

Fecha/hora UTC: 2026-06-30 02:13:13 UTC

## Objetivo

Preparar WPF para consumir Caja API de forma gradual, reversible y controlada por feature flags, sin fallback automatico entre API y SQL.

## Componentes WPF preparados

`CajaApiClient`:

- encapsula lecturas hacia Caja API;
- no ejecuta llamadas si `UseCajaApiRead=false`;
- usa `ApiClientBase` para HTTPS, timeout, bearer token y errores seguros;
- no almacena tokens ni imprime respuestas.

Contratos WPF:

- `CajaTurnoApiResponse`;
- `MovimientoCajaApiResponse`;
- `PreCierreCajaApiResponse`;
- `ResumenMovimientoCajaApiResponse`.

Errores:

- `CajaApiErrorMapper` traduce errores tecnicos a mensajes seguros;
- `401` y sesion vencida limpian sesion por `ApiSessionCoordinator`;
- `403`, `409`, `503`, timeout y red caida se informan sin detalles internos.

Idempotencia futura:

- `CajaOperationCoordinator`;
- `PendingCajaOperation`;
- `CajaOperationState`.

## Contratos API revisados

Endpoints de lectura:

- `GET /api/caja/turnos/abierto?cajaCodigo=...`
  - permiso: `Caja.Ver`;
  - exito con turno: `CajaTurnoResponse`;
  - sin turno abierto: respuesta exitosa sin cuerpo;
  - errores esperados: `401`, `403`, `400`, `503`.

- `GET /api/caja/turnos/{id}/movimientos`
  - permiso: `Caja.Ver`;
  - exito: coleccion de `MovimientoCajaResponse`;
  - errores esperados: `401`, `403`, `404`, `503`.

- `GET /api/caja/turnos/{id}/pre-cierre`
  - permiso: `Caja.Ver`;
  - exito: `PreCierreCajaResponse`;
  - transporta `rowVersion` como string Base64;
  - conflicto si el turno no esta abierto;
  - errores esperados: `401`, `403`, `404`, `409`, `503`.

Endpoints de escritura revisados pero no conectados desde WPF:

- `POST /api/caja/turnos/abrir`, permiso `Caja.Abrir`;
- `POST /api/caja/ingresos`, permiso `Caja.Ingresar`, requiere `Idempotency-Key`;
- `POST /api/caja/retiros`, permiso `Caja.Retirar`, requiere `Idempotency-Key`;
- `POST /api/caja/turnos/{id}/cerrar`, permiso `Caja.Cerrar`, requiere `Idempotency-Key` y `rowVersion`.

## Politica de no fallback

Cuando un flag API de escritura se active en el futuro:

- si API falla, WPF no debe ejecutar SQL historico como respaldo;
- debe mostrar error seguro;
- debe conservar la operacion para reintento controlado;
- debe reutilizar la misma idempotency key mientras la intencion sea la misma;
- no debe mezclar lectura API con escritura SQL historica dentro de la misma accion.

## Datos no visibles en WPF

No deben mostrarse:

- tokens;
- `Idempotency-Key`;
- hashes;
- connection strings;
- SQL;
- identificadores internos innecesarios;
- rowVersion completo;
- trazas tecnicas completas.

