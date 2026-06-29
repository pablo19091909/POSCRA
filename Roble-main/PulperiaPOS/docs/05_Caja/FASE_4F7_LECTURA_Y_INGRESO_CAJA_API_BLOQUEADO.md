# Fase 4F.7 - Lectura de turno e ingreso Caja API bloqueado

Fecha/hora UTC: 2026-06-28 18:14:19 UTC.

## Resultado

Se valido la lectura operativa del turno abierto `CAJA_PRINCIPAL_TEST` desde POS.Api y se preparo el flujo transaccional futuro de ingreso de caja.

No se activo `EnableCajaApiWrite`. No se crearon ingresos, retiros, cierres, ventas, pagos ni movimientos adicionales.

## Endpoints validados

| Endpoint | Resultado |
| --- | --- |
| `GET /api/caja/turnos/abierto?cajaCodigo=CAJA_PRINCIPAL_TEST` | 200 autorizado |
| `GET /api/caja/turnos/{id}/movimientos` | 200 autorizado |
| `GET /api/caja/turnos/{id}/pre-cierre` | 200 autorizado |
| lectura sin token | 401 |
| lectura sin permiso | 403 |

## Turno abierto

La lectura devolvio datos permitidos:

- caja logica `CAJA_PRINCIPAL_TEST`;
- estado `Abierto`;
- fondo inicial `1000.00`;
- fecha UTC;
- sin exponer SQL, host, puerto, token ni detalles internos.

## Pre-cierre

El pre-cierre calculo:

```text
FondoInicial = 1000.00
Ingresos = 0
Retiros = 0
VentasEfectivo = 0
Ajustes = 0
Devoluciones = 0
Diferencias = 0
Efectivo esperado = 1000.00
```

El calculo sale de `movimiento_caja` y no usa `ventas`, `ingreso_caja`, `retiro_caja`, `cierre_caja`, fecha local ni cierre diario.

## Ingreso Caja API

Se preparo `POST /api/caja/ingresos` para una fase futura:

- requiere JWT;
- requiere `Caja.Ingresar`;
- valida feature flag y ambiente antes de abrir transaccion;
- con `EnableCajaApiWrite=false` responde 503 seguro;
- no hace fallback a `ingreso_caja`;
- no toca caja historica.

Pruebas:

| Caso | Resultado |
| --- | --- |
| ingreso sin token | 401 |
| ingreso sin `Caja.Ingresar` | 403 |
| ingreso con permiso y flag apagado | 503 |
| request invalido con flag apagado | 503 |

## Linea base inmediata

| Metrica | Antes | Despues |
| --- | ---: | ---: |
| `caja_turno` | 1 | 1 |
| `movimiento_caja` | 1 | 1 |
| `ingreso_caja` | 9 | 9 |
| `retiro_caja` | 6 | 6 |
| `cierre_caja` | 15 | 15 |
| ventas | 1948 | 1948 |
| pagos | 10 | 10 |
| inventario agregado | 3296 | 3296 |
| saldo agregado en centavos | -295796250 | -295796250 |

## Limitaciones

- WPF no consume Caja API.
- No se implemento escritura real de ingreso en esta fase.
- No hay retiros API.
- No hay cierre API.
- Ventas API no integra `movimiento_caja`.
- No existe idempotencia persistente de caja.

## Recomendacion

Avanzar a Fase 4F.8 para ejecutar un ingreso sintetico controlado en Test, activando `EnableCajaApiWrite` solo de forma local y temporal, con idempotencia operativa definida o aceptando una unica intencion manual estrictamente controlada.
