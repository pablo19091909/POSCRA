# Contrato WPF Retiro Caja API

## Endpoint

`POST /api/caja/retiros`

## Autorizacion

Requiere JWT valido con permiso `Caja.Retirar`.

## Body WPF

| Campo | Tipo | Reglas |
| --- | --- | --- |
| `cajaCodigo` | string | Requerido. Caja logica segura. |
| `monto` | decimal | Requerido, mayor que cero. |
| `motivo` | string | Requerido. |
| `referencia` | string/null | Opcional. |

## Header

| Header | Uso |
| --- | --- |
| `Idempotency-Key` | Requerido por API cuando la escritura esta habilitada. Se envia solo por header. |

## Datos que WPF no envia

- usuario;
- turno;
- fecha;
- estado;
- efectivo disponible;
- hash;
- identificador de movimiento;
- resultado de calculo local.

## Respuesta exitosa

Devuelve un movimiento de caja confirmado. WPF solo debe usarlo como confirmacion de resultado, sin exponer identificadores internos en UI.

## Errores seguros esperados

| Codigo | Interpretacion WPF |
| --- | --- |
| 400 | Solicitud invalida |
| 401 | Sesion vencida |
| 403 | Sin permiso para registrar retiros |
| 404 | No hay recurso de caja disponible |
| 409 | Efectivo insuficiente, turno no disponible o conflicto de caja |
| 503 | Caja API no disponible o escritura apagada |

Con `EnableCajaApiWrite=false`, la API responde `503` antes de iniciar transaccion de escritura.

## Confirmaciones

- La API no escribe en `retiro_caja`.
- La decision final de efectivo disponible pertenece al servidor.
- WPF no debe autorizar definitivamente por calculo local.
- No existe fallback SQL automatico.
