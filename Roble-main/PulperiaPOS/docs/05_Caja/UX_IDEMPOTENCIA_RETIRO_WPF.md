# UX e idempotencia retiro WPF

## Modos

| Flag | Modo |
| --- | --- |
| `UseCajaApiRetiroWrite=false` | Modo historico SQL para registro de retiros |
| `UseCajaApiRetiroWrite=true` | Modo Caja API para registro de retiros |

## Modo historico

Permanece intacto:

- calcula efectivo con `CajaHelper`;
- registra en `retiro_caja`;
- imprime comprobante historico;
- carga historial historico.

## Modo Caja API preparado

Cuando se active en una fase futura:

- muestra fuente Caja API;
- consulta turno abierto;
- consulta pre-cierre;
- muestra efectivo disponible informado por API;
- bloquea boton y campos durante envio;
- no limpia formulario en timeout incierto;
- conserva la misma key para reintentar la misma intencion pendiente;
- genera una key nueva para una intencion nueva;
- refresca estado desde API tras exito.

## Mensajes de UX

- `No hay un turno abierto para registrar el retiro.`
- `El monto solicitado supera el efectivo disponible de la caja o el turno no permite retiros.`
- `No tiene permiso para registrar retiros de caja.`
- `No fue posible confirmar el resultado de la operacion. Revise la conexion y reintente sin cambiar los datos.`
- `No fue posible comunicarse con el servicio de caja. Intente nuevamente.`

## Reglas de seguridad

- No mostrar stack trace.
- No mostrar endpoint, host ni puerto.
- No mostrar key, hash, SQL, rowVersion ni identificadores internos.
- No registrar cuerpo completo del request.
- No imprimir comprobante historico desde ruta API.
