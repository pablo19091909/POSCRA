# Auditoria de inconsistencias - venta, caja e inventario

## Lecturas implementadas

`GET /api/reportes/auditoria/inconsistencias` detecta conteos agregados de:

- venta efectiva con pago API sin movimiento `VentaEfectivo`;
- movimiento `VentaEfectivo` sin pago valido;
- reversa confirmada sin movimiento compensatorio;
- movimiento `Reversa` huerfano;
- idempotencia de venta en proceso;
- idempotencia de caja en proceso;
- doble reversa para una venta;
- doble movimiento `VentaEfectivo` para un pago.

## Interpretacion

Las inconsistencias son alertas de revision. No corrigen datos y no ejecutan escrituras.

## Validacion con evidencia Test

La venta de prueba reversada permanece consistente:

- venta original visible;
- una reversa;
- un movimiento compensatorio;
- inventario restaurado;
- sin reversas huerfanas;
- sin idempotencias pendientes.

## Limitaciones conocidas

- Inventario restaurado se valida por relacion venta/reversa y no por reconstruccion completa historica de todos los productos.
- Datos historicos previos pueden generar alertas si no comparten semantica API.
- No se modifica ningun dato durante esta fase.


