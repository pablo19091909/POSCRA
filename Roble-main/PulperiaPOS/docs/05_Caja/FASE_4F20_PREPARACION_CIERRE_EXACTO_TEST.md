# Fase 4F.20 - Preparacion cierre exacto Test

Fecha UTC: 2026-06-29 22:57:11 UTC

## Alcance

Se ejecuto validacion ampliada no destructiva para preparar un cierre exacto futuro del turno `CAJA_PRINCIPAL_TEST`.

No se activo `EnableCajaApiWrite`, no se cerro el turno, no se crearon movimientos, idempotencias ni cierres, y no se modifico WPF.

## Ambiente y flags

- `Environment=Test`: confirmado.
- `writes_allowed_for_testing=1`: confirmado.
- `UseVentasApiWrite=false`.
- `EnableVentasApiWrite=false`.
- `EnableCajaApiWrite=false`.

Durante la validacion hubo una indisponibilidad transitoria de Azure SQL. Se reintento y la base respondio correctamente sin cambios de datos.

## Linea base

Antes y despues:

- `caja_turno=1`.
- `movimiento_caja=5`.
- `caja_idempotencia=4`.
- `FondoInicial=1`.
- `IngresoCaja=2`.
- `RetiroCaja=2`.
- `CierreDiferencia=0`.
- `IngresoCaja Completada=2`.
- `RetiroCaja Completada=2`.
- `CerrarTurno Completada=0`.
- `ingreso_caja=9`.
- `retiro_caja=6`.
- `cierre_caja=15`.
- `EfectivoEsperado=201.00`.

El turno sigue `Abierto`. No existen turnos `EnCierre` ni `Cerrado` asociados a la caja Test. No existen idempotencias `EnProceso` ni `Fallida`.

## Lecturas seguras

Rutas validadas:

- `GET /api/caja/turnos/abierto?cajaCodigo=CAJA_PRINCIPAL_TEST`.
- `GET /api/caja/turnos/{id}/movimientos`.
- `GET /api/caja/turnos/{id}/pre-cierre`.

Resultados:

- sin token: `401`;
- token sin `Caja.Ver`: `403`;
- token con `Caja.Ver`: `200`;
- `rowVersion` presente y valido como Base64 de 8 bytes;
- no se imprimio el valor completo de `rowVersion`;
- lecturas no cambiaron `row_version`, estado, movimientos ni idempotencias.

## Cierre exacto planeado

```text
EfectivoEsperado = 201.00
EfectivoContadoPlaneado = 201.00
DiferenciaEsperada = 0.00
CierreDiferenciaEsperado = no crear
EstadoFinalEsperado = Cerrado
```

El calculo se basa exclusivamente en `movimiento_caja`, excluye `CierreDiferencia`, no usa fecha local ni tablas historicas.

## Seguridad del endpoint de cierre

Con `EnableCajaApiWrite=false`:

- sin token: `401`;
- token sin `Caja.Cerrar`: `403`;
- token con `Caja.Cerrar`, sin key: `503`;
- token con `Caja.Cerrar`, key invalida: `503`;
- token con `Caja.Cerrar`, key valida y body incompleto: `503`;
- token con `Caja.Cerrar`, key valida y body valido: `503`.

Esto confirma que el flag se evalua antes de validar body, key, `rowVersion` o iniciar transaccion de escritura.

## Pruebas puras

- `rowVersion` Base64 valido de 8 bytes: aceptado.
- Base64 invalido: rechazado.
- Base64 valido con longitud distinta a 8 bytes: rechazado.
- mismo request genera hash estable.
- cambio de contado, observacion, turno o `rowVersion` cambia hash.
- contado `201.00` produce diferencia `0.00`.
- diferencia cero no crea `CierreDiferencia`.
- sobrante/faltante producen monto absoluto positivo para auditoria futura.

## Limitaciones

- Sin reversas.
- Sin ventas efectivas integradas a Caja API.
- Sin WPF Caja API.
- Sin prueba real de sobrante/faltante.
- Sin Dolares, Donacion ni pagos combinados.

## Recomendacion

Continuar con Fase 4F.21: prueba controlada de cierre exacto del turno Test, con autorizacion explicita para activar temporalmente `EnableCajaApiWrite=true`.
