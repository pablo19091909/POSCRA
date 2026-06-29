# Fase 4F.6 - Prueba de apertura real de turno Caja API en Test

Fecha/hora UTC: 2026-06-28 18:02:40 UTC.

## Entorno

Se confirmo `Environment=Test` con `writes_allowed_for_testing=1`.

La activacion de `EnableCajaApiWrite=true` se hizo solo como variable de entorno temporal del proceso de POS.Api. No se modificaron archivos versionados para activar escritura.

## Flags

Estado versionado/local confirmado antes y despues:

- `UseVentasApiWrite=false`
- `EnableVentasApiWrite=false`
- `EnableCajaApiWrite=false`

Durante la ejecucion, Caja API estuvo activa solo dentro del proceso temporal de POS.Api.

## Linea base inmediata

| Metrica | Antes | Despues |
| --- | ---: | ---: |
| `caja_turno` | 0 | 1 |
| `movimiento_caja` | 0 | 1 |
| `ingreso_caja` | 9 | 9 |
| `retiro_caja` | 6 | 6 |
| `cierre_caja` | 15 | 15 |
| ventas | 1948 | 1948 |
| pagos | 10 | 10 |
| inventario agregado | 3296 | 3296 |
| saldo agregado en centavos | -295796250 | -295796250 |

No se detecto actividad concurrente externa durante la ventana de prueba.

## Apertura

Endpoint usado:

```text
POST /api/caja/turnos/abrir
```

Entrada funcional:

- `cajaCodigo=CAJA_PRINCIPAL_TEST`
- `fondoInicial=1000.00`
- observacion sintetica de Fase 4F.6

Resultado:

- HTTP 200;
- estado `Abierto`;
- caja `CAJA_PRINCIPAL_TEST`;
- fondo inicial `1000.00`;
- fecha UTC generada por servidor.

No se expusieron token, credenciales, connection strings, usuario individual ni datos internos.

## Validacion posterior

Se confirmo:

- existe exactamente un turno para `CAJA_PRINCIPAL_TEST`;
- existe exactamente un turno `Abierto`;
- existe exactamente un turno `Abierto` o `EnCierre`;
- el fondo inicial del turno es `1000.00`;
- el usuario de apertura es valido y activo;
- `apertura_utc` esta presente;
- los campos de cierre permanecen `NULL`;
- `row_version` esta presente;
- existe exactamente un movimiento asociado al turno;
- existe exactamente un movimiento `FondoInicial`;
- el movimiento tiene monto `1000.00`;
- la moneda es `CRC`;
- la fecha UTC esta presente;
- el usuario del movimiento es valido y activo;
- no hay factura, pago, ingreso, retiro ni reversa asociados;
- no hay movimientos huerfanos;
- no hay turnos sin fondo inicial;
- no hay fondo inicial duplicado.

## Conflicto

Se ejecuto una segunda solicitud controlada para la misma caja.

Resultado:

- HTTP 409;
- no creo segundo turno;
- no creo segundo `FondoInicial`;
- no modifico caja historica;
- no expuso informacion sensible.

## Estado final

POS.Api fue detenido al finalizar y el puerto `7046` quedo libre.

El turno Test y su movimiento `FondoInicial` permanecen como evidencia y base para la siguiente fase. No se ejecuto rollback ni eliminacion.

## Limitaciones vigentes

- WPF no consume Caja API.
- No hay ingresos API.
- No hay retiros API.
- No hay pre-cierre/cierre API.
- Ventas API no integra `movimiento_caja`.
- No existe idempotencia persistente de caja.
- No hay anulacion ni reversa implementada.
- Dolares, donacion y pagos combinados quedan fuera de alcance.

## Recomendacion

Avanzar a Fase 4F.7 para validar lectura operativa del turno abierto desde POS.Api y preparar, sin conectar WPF aun, el flujo de ingreso o retiro API bloqueado por feature flag.
