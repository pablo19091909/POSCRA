# Fase 4F.11 - Idempotencia ingreso Caja implementada y bloqueada

Fecha/hora UTC: 2026-06-28 21:32:40 UTC.

## Resultado

Se integro `Idempotency-Key` en el flujo interno de `POST /api/caja/ingresos`.

La integracion esta completa en codigo, pero permanece bloqueada por `EnableCajaApiWrite=false`. Con el flag apagado no se parsea la key, no se calcula hash, no se abre transaccion y no se escribe en `caja_idempotencia` ni en `movimiento_caja`.

## Componentes modificados

- `POS.Api/Controllers/CajaController.cs`
- `POS.Api/Application/Caja/ICajaService.cs`
- `POS.Api/Application/Caja/CajaService.cs`
- `POS.Api/Application/Caja/ICajaRepository.cs`
- `POS.Api/Application/Caja/ICajaIdempotencyService.cs`
- `POS.Api/Application/Caja/CajaIdempotencyService.cs`
- `POS.Api/Infrastructure/Data/Caja/CajaRepository.cs`
- `POS.Api/Program.cs`

## Contrato

Para escrituras futuras de ingreso:

```text
Idempotency-Key: <GUID>
```

Reglas:

- requerido solo cuando la escritura este permitida;
- formato GUID valido;
- no puede ser `Guid.Empty`;
- no se acepta desde body;
- no se registra en logs;
- no se devuelve completo en respuestas.

## Pruebas no destructivas

Con `EnableCajaApiWrite=false`:

| Caso | Resultado |
| --- | ---: |
| sin token | 401 |
| token sin `Caja.Ingresar` | 403 |
| token con permiso sin key | 503 |
| token con permiso key invalida | 503 |
| token con permiso key valida | 503 |

Health checks:

- `/health=200`
- `/health/database=200`
- `/api/system/version=200`

## Pruebas puras

Resultado:

- GUID valido aceptado;
- GUID vacio rechazado;
- GUID invalido rechazado;
- header ausente rechazado por parser;
- hash de ingreso mide 32 bytes;
- mismo request normalizado produce mismo hash;
- cambio de monto cambia hash;
- cambio de caja cambia hash;
- cambio de motivo cambia hash;
- cambio de referencia cambia hash;
- cambio de usuario cambia hash.

## Integridad

| Metrica | Antes | Despues |
| --- | ---: | ---: |
| `caja_idempotencia` | 0 | 0 |
| `caja_turno` | 1 | 1 |
| `movimiento_caja` | 1 | 1 |
| ingresos API | 0 | 0 |
| retiros API | 0 | 0 |
| cierres API | 0 | 0 |
| `ingreso_caja` | 9 | 9 |
| `retiro_caja` | 6 | 6 |
| `cierre_caja` | 15 | 15 |

## Limitaciones

- No se activo Caja API.
- No se creo ingreso real.
- No se creo registro de idempotencia.
- No se conecto WPF.
- No se implemento limpieza automatica de `EnProceso`.

## Recomendacion

Avanzar a Fase 4F.12 para ejecutar una prueba unica de ingreso sintetico en Test con `Idempotency-Key`, activando `EnableCajaApiWrite` solo local y temporalmente, y validando reintento idempotente sin duplicar movimiento.
