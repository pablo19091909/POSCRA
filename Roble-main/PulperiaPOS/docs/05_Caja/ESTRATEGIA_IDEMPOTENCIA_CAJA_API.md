# Estrategia de idempotencia Caja API

## Estado actual

No se creo tabla de idempotencia de caja en Fase 4F.4. No se insertaron llaves ni registros.

## Recomendacion futura

Crear idempotencia separada para caja, distinta de `venta_idempotencia`, antes de permitir reintentos de:

- apertura de turno;
- ingreso;
- retiro;
- cierre.

## Reglas futuras

- La llave de idempotencia debe pertenecer a usuario, caja logica, tipo de operacion y payload normalizado.
- Un reintento exitoso no debe crear doble ingreso/retiro/cierre.
- Un cierre en progreso debe responder conflicto seguro.
- No registrar cuerpos completos de request ni secretos en logs.
- No usar idempotencia de ventas para caja.

## Pendiente

Disenar `caja_idempotencia` o mecanismo equivalente antes de activar escritura real de ingresos, retiros o cierres.
