# Contrato WPF - Ingreso Caja API

Fecha/hora UTC: 2026-06-30T13:45:44Z

## Endpoint

`POST /api/caja/ingresos`

## Autorizacion

Requiere JWT valido con permiso `Caja.Ingresar`.

## Header requerido

`Idempotency-Key`

La key se genera localmente por `CajaOperationCoordinator` y no se imprime, registra ni muestra.

## Body

| Campo | Tipo | Requerido | Origen WPF |
| --- | --- | --- | --- |
| `cajaCodigo` | string | Si | Caja logica controlada |
| `monto` | decimal | Si | Campo monto |
| `motivo` | string | Si | Campo motivo |
| `referencia` | string/null | No | Campo referencia API |

## Campos que WPF no envia

- usuario;
- turno;
- fecha;
- estado;
- hash;
- efectivo esperado;
- id de movimiento;
- rowVersion.

## Respuesta

`MovimientoCajaResponse` con datos seguros del movimiento confirmado por API.

## Comportamiento con escritura apagada

Con `EnableCajaApiWrite=false`, la API responde `503` antes de iniciar transaccion de escritura. No crea movimiento ni idempotencia.

## Validaciones WPF

| Validacion | Resultado |
| --- | --- |
| Monto no numerico | Bloqueado localmente |
| Monto cero | Bloqueado localmente |
| Monto negativo | Bloqueado localmente |
| Motivo vacio | Bloqueado localmente |
| Motivo mayor a 250 | Bloqueado localmente |
| Referencia mayor a 250 | Bloqueado localmente |

## Validaciones API confirmadas

| Caso | HTTP |
| --- | --- |
| Sin token | 401 |
| Sin permiso | 403 |
| Escritura apagada | 503 |
