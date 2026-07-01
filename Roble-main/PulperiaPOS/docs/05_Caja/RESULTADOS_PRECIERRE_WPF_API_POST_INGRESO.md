# Resultados pre-cierre WPF/API post ingreso

Fecha UTC: 2026-06-30.

## Pre-cierre confirmado

El pre-cierre del turno abierto de Test se valido por API y por WPF.

| Concepto | Valor |
| --- | ---: |
| FondoInicial | 1000.00 |
| IngresoCaja | 100.00 |
| RetirosCaja | 0.00 |
| EfectivoEsperado | 1100.00 |

## Resumen API

La API devolvio resumen consistente:

| Tipo | Cantidad | Total |
| --- | ---: | ---: |
| FondoInicial | 1 | 1000.00 |
| IngresoCaja | 1 | 100.00 |

## Resultado WPF

El operador valido visualmente el resumen en pantallas de caja. Se corrigio el formato visual para evitar mostrar `cantidad/total`, porque podia interpretarse como una fraccion o valor incorrecto.

Formato corregido:

`Resumen: FondoInicial: 1000.00; IngresoCaja: 100.00.`

## Integridad

La validacion no modifico:

- turno;
- movimientos;
- idempotencias;
- historicos de ingreso/retiro/cierre;
- ventas;
- pagos;
- inventario;
- saldos de cliente.
