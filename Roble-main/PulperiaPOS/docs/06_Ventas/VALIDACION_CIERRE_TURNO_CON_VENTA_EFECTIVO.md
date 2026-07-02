# Validacion cierre turno con VentaEfectivo

## Pre-cierre

Previo al cierre exacto:

- Turno abierto: 1.
- Turno en cierre: 0.
- Fondo inicial: 1000.00.
- `VentaEfectivo`: 2 movimientos.
- Total `VentaEfectivo`: 1510.00.
- Efectivo esperado: 2510.00.
- Sin ingresos manuales.
- Sin retiros.
- Sin ajustes.
- Sin reversas.
- Sin `CierreDiferencia`.

## Cierre WPF

El cierre se ejecuto desde `CierreCajaPage` WPF por Caja API.

Valores finales:

- Estado final del turno: cerrado.
- Efectivo esperado: 2510.00.
- Efectivo contado: 2510.00.
- Diferencia: 0.00.
- `CierreDiferencia`: 0.

## Idempotencia

- `CerrarTurno Completada`: 1.
- `caja_idempotencia` en proceso: 0.
- `caja_idempotencia` fallida: 0.

## Historicos

Sin cambios en:

- `ingreso_caja`.
- `retiro_caja`.
- `cierre_caja`.

## Confirmacion

El cierre exacto valido que Caja API calcula correctamente el efectivo esperado con:

1000.00 fondo inicial + 10.00 venta API previa + venta WPF efectiva = 2510.00.
