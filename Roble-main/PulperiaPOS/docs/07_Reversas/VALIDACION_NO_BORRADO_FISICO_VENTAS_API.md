# Validacion de no borrado fisico para ventas API

## Cambio aplicado

`VentasCrudWindow` conserva su ruta historica, pero cuando `UseVentasApiReversaWrite=true` y la venta seleccionada es API efectivo, el borrado fisico queda bloqueado con mensaje seguro.

## Confirmacion

No se elimino ninguna venta durante esta fase.

## Pendiente

Validar visualmente en WPF que una venta API efectivo elegible muestra el flujo de reversa y no permite borrado fisico en modo reversa activo.
