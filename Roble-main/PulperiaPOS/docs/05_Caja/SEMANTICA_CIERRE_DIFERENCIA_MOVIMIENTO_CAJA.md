# Semantica CierreDiferencia movimiento caja

Fecha UTC: 2026-06-29 21:37:22 UTC

## Regla implementada

```text
Diferencia = EfectivoContado - EfectivoEsperado
```

Si `Diferencia = 0`:

- no se crea `CierreDiferencia`;
- el turno se cierra con diferencia `0.00`.

Si `Diferencia != 0`:

- se requiere observacion;
- se crea un movimiento `CierreDiferencia`;
- `movimiento_caja.monto = ABS(Diferencia)`;
- `caja_turno.diferencia` conserva el signo.

## Direccion financiera

- `diferencia > 0`: sobrante.
- `diferencia < 0`: faltante.

El esquema actual exige `movimiento_caja.monto > 0`, por lo que el signo no se guarda en el movimiento. La direccion queda en `caja_turno.diferencia`.

## Auditoria

`CierreDiferencia` es un evento de auditoria de cierre. No reescribe ingresos, retiros, ventas ni movimientos previos.

## Pre-cierre

El calculo de efectivo esperado excluye explicitamente `CierreDiferencia`, tanto en lectura como dentro de la transaccion de cierre. Esto evita que el cierre altere retrospectivamente el efectivo esperado previo.

## Limites

La semantica no modifica el esquema. Si en el futuro se desea representar direccion directamente en movimientos, debe ser una migracion separada y controlada.
