# Plan prueba Retiro WPF Caja API

## Objetivo futuro

Validar un unico retiro sintetico desde `RetirosCajaPage` WPF por Caja API, solo cuando exista autorizacion expresa para activar escrituras.

## Precondiciones actuales

- `Environment=Test`.
- `UseCajaApiRead=true`.
- `UseCajaApiRetiroWrite=false`.
- `EnableCajaApiWrite=false`.
- Turno Test abierto.
- Efectivo esperado actual: `1100.00`.

## Antes de activar escritura

1. Confirmar agregados de base.
2. Confirmar flags apagados.
3. Confirmar health checks.
4. Confirmar usuario existente con `Caja.Retirar`.
5. Confirmar que `retiro_caja`, `movimiento_caja` y `caja_idempotencia` no cambian con flag apagado.

## Prueba futura autorizada

Solo en fase posterior:

1. Activar temporalmente `UseCajaApiRetiroWrite=true`.
2. Activar temporalmente `EnableCajaApiWrite=true` solo en proceso POS.Api.
3. Abrir `RetirosCajaPage`.
4. Validar modo Caja API visible.
5. Registrar un retiro sintetico unico autorizado.
6. Confirmar un solo movimiento `RetiroCaja`.
7. Confirmar una sola idempotencia `RetiroCaja Completada`.
8. Confirmar que `retiro_caja` historico no cambia.
9. Restaurar flags.
10. Detener POS.Api.

## Casos pendientes

- retiro valido;
- doble clic;
- timeout;
- efectivo insuficiente;
- sin turno abierto;
- turno en cierre;
- turno cerrado;
- API caida;
- usuario sin permiso.

## Fuera de alcance actual

- cierre WPF por API;
- reversas;
- ventas efectivo integradas con Caja API;
- Dolares, Donacion y pagos combinados.
