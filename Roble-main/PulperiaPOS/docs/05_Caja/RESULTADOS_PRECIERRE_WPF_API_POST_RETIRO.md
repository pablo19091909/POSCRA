# Resultados pre-cierre WPF/API post retiro

Fecha UTC: 2026-06-30

## Resumen financiero esperado

Despues del retiro sintetico de Fase 4F.32, el turno abierto de `CAJA_PRINCIPAL_TEST` debe conservar:

- FondoInicial: 1000.00.
- IngresosCaja: 100.00.
- RetirosCaja: 100.00.
- EfectivoEsperado: 1000.00.
- Estado: Abierto.

## Validacion por lecturas agregadas

Se confirmo:

- Fondo inicial: 1000.00.
- Ingresos del turno: 100.00.
- Retiros del turno: 100.00.
- Efectivo esperado: 1000.00.
- Un movimiento `FondoInicial`.
- Un movimiento `IngresoCaja` de 100.00.
- Un movimiento `RetiroCaja` de 100.00.
- Sin ajustes.
- Sin reversas.
- Sin `CierreDiferencia` asociado al turno abierto.

## Validacion visual

El operador confirmo revision visual de `IngresoCajaPage`, `RetirosCajaPage` y `CierreCajaPage` con API disponible.

Limitacion: `CierreCajaPage` todavia no esta implementada sobre `CajaApiClient`; por codigo conserva calculos historicos. Esta pantalla debe migrarse en una fase posterior antes de considerar el pre-cierre WPF completamente API-first.

## API caida

El operador confirmo validacion de API caida. No se reportaron datos tecnicos expuestos ni escrituras accidentales.
