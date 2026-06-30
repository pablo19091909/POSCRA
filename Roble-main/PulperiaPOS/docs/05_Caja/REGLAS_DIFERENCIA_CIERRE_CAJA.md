# Reglas de diferencia cierre Caja API

Fecha UTC: 2026-06-29 21:12:41 UTC

## Formula

```text
EfectivoEsperado = sumatoria neta de movimiento_caja confirmado del turno
Diferencia = EfectivoContado - EfectivoEsperado
```

Tipos positivos:

- `FondoInicial`;
- `VentaEfectivo`;
- `IngresoCaja`;
- `AjustePositivo`.

Tipos negativos:

- `RetiroCaja`;
- `AjusteNegativo`;
- `DevolucionEfectivo`.

`CierreDiferencia` no debe alterar el efectivo esperado previo al cierre. Sirve como evidencia de cierre, no como reescritura del saldo calculado.

## Turno Test actual

```text
FondoInicial: 1000.00
IngresosCaja: 501.00
RetirosCaja: 1300.00
EfectivoEsperado: 201.00
```

## Diferencia cero

```text
EfectivoContado = 201.00
Diferencia = 0.00
```

Resultado futuro:

- cerrar turno;
- no crear `CierreDiferencia`;
- guardar esperado, contado y diferencia cero;
- completar idempotencia `CerrarTurno`.

## Sobrante

```text
EfectivoContado > EfectivoEsperado
Diferencia > 0
```

Resultado futuro:

- observacion obligatoria;
- crear `CierreDiferencia` por el valor absoluto de la diferencia;
- guardar diferencia positiva en `caja_turno.diferencia`;
- cerrar turno;
- completar idempotencia.

## Faltante

```text
EfectivoContado < EfectivoEsperado
Diferencia < 0
```

Resultado futuro:

- observacion obligatoria;
- crear `CierreDiferencia` por el valor absoluto de la diferencia;
- guardar diferencia negativa en `caja_turno.diferencia`;
- cerrar turno;
- completar idempotencia.

## Decision de signo

`movimiento_caja.monto` tiene constraint `monto > 0`; por tanto `CierreDiferencia` no puede usar monto firmado sin modificar el modelo. La regla elegida es:

- `movimiento_caja.monto = ABS(Diferencia)`;
- `caja_turno.diferencia` conserva el signo;
- `observacion_cierre` explica el motivo cuando la diferencia no es cero.

Esta regla es compatible con los checks actuales y evita alterar retrospectivamente ingresos, retiros o ventas previas.

## Validaciones

- `efectivoContado` no puede ser negativo.
- Diferencia distinta de cero requiere observacion no vacia.
- Observacion maxima: 250 caracteres.
- No usar `double`, `float` ni cultura local.
- Usar `decimal(18,2)` y UTC.
