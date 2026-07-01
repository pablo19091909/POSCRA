# Plan de prueba - Cierre WPF Caja API

Fecha UTC: 2026-06-30

## Antes de habilitar escritura

Confirmar:

- Turno abierto `CAJA_PRINCIPAL_TEST`.
- `UseCajaApiCierreWrite=false`.
- `EnableCajaApiWrite=false`.
- Ventas API apagada.
- Fondo inicial 1000.00.
- Ingresos 100.00.
- Retiros 100.00.
- Efectivo esperado 1000.00.
- Sin `EnCierre`.
- Sin `CierreDiferencia` en turno abierto.

## Pruebas puras pendientes

- Contado exacto valido.
- Contado con sobrante.
- Contado con faltante.
- Contado negativo invalido.
- Observacion vacia con diferencia invalida.
- Observacion valida con diferencia.
- Observacion valida en cierre exacto.
- `rowVersion` vacio invalido.
- `rowVersion` invalido.
- Misma intencion conserva key.
- Intencion nueva genera key distinta.
- Doble clic bloqueado.
- Timeout conserva formulario y key.
- `409` restaura UI sin SQL.
- `503` restaura UI sin SQL.

## Primera prueba destructiva futura

La primera prueba real debe hacerse en una fase separada, con autorizacion expresa para:

- Activar temporalmente `UseCajaApiCierreWrite=true`.
- Activar temporalmente `EnableCajaApiWrite=true`.
- Cerrar el turno Test.
- Crear como maximo un `CierreDiferencia` si corresponde.
- Crear una idempotencia `CerrarTurno Completada`.

## Validacion posterior esperada

- Turno pasa a `Cerrado`.
- No queda `EnCierre`.
- Efectivo contado, esperado y diferencia vienen de API.
- `cierre_caja` historico no cambia.
- Ingresos/retiros quedan bloqueados por API tras cierre.
- No hay rollback ni limpieza.
