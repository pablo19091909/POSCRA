# Validacion rowVersion y contrato cierre

Fecha UTC: 2026-06-29 22:57:11 UTC

## Contrato de lectura

Las respuestas de turno abierto y pre-cierre entregan `rowVersion` para el cierre futuro. La validacion confirmo:

- formato Base64 valido;
- longitud decodificada de 8 bytes;
- no se expuso el valor completo en reporte;
- no cambio tras lecturas.

## Contrato de cierre futuro

Endpoint:

```text
POST /api/caja/turnos/{id}/cerrar
```

Body:

```json
{
  "efectivoContado": 201.00,
  "observacion": null,
  "rowVersion": "<Base64 de 8 bytes>"
}
```

Header:

```text
Idempotency-Key: <GUID>
```

## Validaciones puras

| Caso | Resultado |
| --- | --- |
| Base64 valido 8 bytes | aceptado |
| Base64 invalido | rechazado |
| Base64 valido con longitud distinta a 8 bytes | rechazado |
| cambio de `rowVersion` | hash distinto |
| cambio de efectivo contado | hash distinto |
| cambio de observacion | hash distinto |
| cambio de turno | hash distinto |
| mismo request | hash estable |

## Seguridad

Con escritura apagada, cierre responde `503` antes de validar key, body o `rowVersion`. Esto evita transacciones o idempotencias accidentales durante preparacion.

## Cierre exacto

Para el turno actual:

- `EfectivoEsperado=201.00`;
- `EfectivoContadoPlaneado=201.00`;
- `Diferencia=0.00`;
- observacion no obligatoria para diferencia cero;
- no debe crear `CierreDiferencia`.

## Riesgos pendientes

La prueba real debe obtener el `rowVersion` inmediatamente antes del cierre. Cualquier movimiento concurrente cambiaria la version y debe bloquear la operacion con conflicto seguro.
