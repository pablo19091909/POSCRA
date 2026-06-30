# Plan prueba cierre exacto F4F.21

Fecha UTC: 2026-06-29 22:57:11 UTC

## Objetivo

Ejecutar, solo con autorizacion explicita futura, un cierre exacto del turno Test actual.

## Preparacion

1. Confirmar linea base:
   - turno `Abierto`;
   - movimientos `5`;
   - idempotencias `4`;
   - `CierreDiferencia=0`;
   - `CerrarTurno Completada=0`;
   - `EfectivoEsperado=201.00`.
2. Iniciar POS.Api con:
   - `EnableCajaApiWrite=true` solo como variable temporal del proceso;
   - `EnableVentasApiWrite=false`.
3. Obtener `rowVersion` vigente inmediatamente antes de cerrar.
4. Generar un `Idempotency-Key` GUID nuevo, sin imprimirlo.

## Solicitud principal

Enviar:

```text
POST /api/caja/turnos/{id}/cerrar
```

Body:

```json
{
  "efectivoContado": 201.00,
  "observacion": null,
  "rowVersion": "<vigente>"
}
```

Resultado esperado:

- HTTP exitoso;
- turno `Cerrado`;
- `efectivo_esperado=201.00`;
- `efectivo_contado=201.00`;
- `diferencia=0.00`;
- `CierreDiferencia=0`;
- una idempotencia `CerrarTurno Completada`;
- tablas historicas sin cambios.

## Repeticion idempotente

Repetir misma key y mismo body:

- resultado equivalente;
- sin segundo cierre;
- sin segunda idempotencia;
- sin movimiento adicional.

Misma key con body distinto:

- `409`;
- sin alteracion de cierre.

## Bloqueos posteriores

Intentar ingreso o retiro con nueva key sobre turno cerrado:

- rechazo seguro;
- sin movimiento;
- sin idempotencia completada para operacion rechazada.

## Restauracion

- detener POS.Api;
- confirmar puerto libre;
- reiniciar solo con `EnableCajaApiWrite=false`;
- confirmar flags finales apagados;
- conservar evidencia Test, sin rollback ni deletes.

## No ejecutar en 4F.20

Este plan no fue ejecutado en 4F.20. El cierre real queda pendiente para Fase 4F.21 con autorizacion explicita.
