# Fase 4F.19 - Cierre turno API implementado bloqueado

Fecha UTC: 2026-06-29 21:37:22 UTC

## Alcance

Se implemento en POS.Api el flujo interno de cierre transaccional e idempotente de `POST /api/caja/turnos/{id}/cerrar`.

La escritura permanece bloqueada por `EnableCajaApiWrite=false`. Durante esta fase no se cerro el turno Test, no se creo `CierreDiferencia`, no se creo idempotencia `CerrarTurno` y no se modifico WPF.

## Archivos principales modificados

- `POS.Api/Application/Caja/ICajaService.cs`.
- `POS.Api/Application/Caja/ICajaRepository.cs`.
- `POS.Api/Application/Caja/ICajaIdempotencyService.cs`.
- `POS.Api/Application/Caja/CajaIdempotencyService.cs`.
- `POS.Api/Application/Caja/CajaService.cs`.
- `POS.Api/Application/Caja/CierreTurnoQuery.cs`.
- `POS.Api/Contracts/Caja/CierreCajaResponse.cs`.
- `POS.Api/Contracts/Caja/PreCierreCajaResponse.cs`.
- `POS.Api/Controllers/CajaController.cs`.
- `POS.Api/Infrastructure/Data/Caja/CajaRepository.cs`.

## Implementacion

El endpoint de cierre:

- requiere JWT;
- requiere `Caja.Cerrar`;
- valida `EnableCajaApiWrite` y ambiente antes de validar key/body;
- con flag apagado responde `503`;
- cuando escritura este habilitada, exige `Idempotency-Key`;
- requiere `rowVersion`;
- usa transaccion `Serializable`;
- no usa `cierre_caja` historico;
- no modifica WPF.

## Resultado de seguridad con flag apagado

- sin token: `401`;
- token sin `Caja.Cerrar`: `403`;
- token autorizado sin key: `503`;
- token autorizado con key invalida: `503`;
- token autorizado con key valida: `503`.

## Integridad

Linea base antes y despues sin cambios:

- `caja_turno=1`;
- `movimiento_caja=5`;
- `caja_idempotencia=4`;
- `ingreso_caja=9`;
- `retiro_caja=6`;
- `cierre_caja=15`;
- `CierreDiferencia=0`;
- `EfectivoEsperado=201.00`;
- turno Test sigue `Abierto`.

## Diferencia contra cierre historico

El cierre historico WPF usa `cierre_caja`, fecha/hora local y calculos por tablas historicas. El cierre API implementado usa turno, UTC, `row_version`, idempotencia y `movimiento_caja`.

## Pendiente

- No se ha ejecutado un cierre real.
- No se ha probado cierre exacto/sobrante/faltante con escritura habilitada.
- WPF sigue desconectado de Caja API.
- Ventas efectivo aun no integran `movimiento_caja`.

## Recomendacion

Continuar con Fase 4F.20: validacion no destructiva ampliada y preparacion de prueba sintetica de cierre exacto en Test, manteniendo autorizacion explicita antes de activar `EnableCajaApiWrite`.
