# UX e idempotencia - Ingreso WPF Caja API

Fecha/hora UTC: 2026-06-30T13:45:44Z

## Modo visual

`IngresoCajaPage` muestra el origen operativo:

| Flag | Modo |
| --- | --- |
| `UseCajaApiIngresoWrite=false` | Modo historico SQL para registro de ingresos |
| `UseCajaApiIngresoWrite=true` | Modo Caja API para registro de ingresos |

## Doble clic

Cuando el modo API este activo:

- el boton se deshabilita durante el envio;
- monto, motivo y referencia quedan deshabilitados durante el envio;
- `CajaOperationCoordinator.TryBegin` evita una segunda operacion simultanea;
- la misma intencion reutiliza la misma key mientras corresponda.

## Intencion de ingreso

La intencion se calcula con:

- caja logica;
- monto normalizado;
- motivo;
- referencia;
- usuario de sesion.

## Timeout

Ante timeout:

- no se limpia el formulario;
- se conserva la intencion;
- se muestra mensaje de resultado incierto;
- no se afirma exito ni fallo definitivo.

## Red caida

Ante error de red:

- se permite reintentar la misma operacion;
- no se crea fallback SQL;
- se muestra mensaje seguro de servicio no disponible.

## Exito futuro

Cuando la fase de escritura sea autorizada y la API confirme exito:

- se limpia el formulario;
- se refresca el estado API;
- se informa que el ingreso fue registrado por Caja API.

Esta fase no ejecuto exito real porque la escritura permanecio apagada.
