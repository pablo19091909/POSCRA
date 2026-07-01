# UX e idempotencia - Cierre WPF Caja API

Fecha UTC: 2026-06-30

## Modo API

Cuando `UseCajaApiCierreWrite=true`, la pantalla debe mostrar:

- `Modo Caja API`.
- Estado del turno.
- Ingresos.
- Retiros.
- Efectivo esperado.
- Diferencia visual estimada.
- Efectivo contado editable.
- Observacion editable.

## Campos no editables

No debe permitir editar:

- Turno.
- Usuario.
- Fecha.
- Efectivo esperado.
- Diferencia final.
- Estado.
- `rowVersion`.
- `Idempotency-Key`.
- Hash.
- Identificadores tecnicos.

## Confirmacion

Antes de enviar cierre API, WPF debe pedir confirmacion explicita.

La diferencia visual es solo orientativa; el resultado oficial depende de API.

## Idempotencia

La intencion se calcula con:

- Turno.
- Efectivo contado.
- Observacion.
- `rowVersion`.
- Usuario de sesion.

La misma intencion pendiente conserva key. Una intencion distinta genera key nueva.

## Timeout

Ante timeout:

- No limpiar efectivo contado.
- No limpiar observacion.
- Conservar intencion pendiente.
- Pedir reintento sin cambiar datos.
- No afirmar exito.
- No ejecutar fallback SQL.

## Doble clic

`CajaOperationCoordinator` bloquea una segunda ejecucion local mientras la operacion esta en progreso.

Ademas, la UI deshabilita:

- Boton Guardar Cierre.
- Boton Volver.
- Efectivo contado.
- Observaciones.
