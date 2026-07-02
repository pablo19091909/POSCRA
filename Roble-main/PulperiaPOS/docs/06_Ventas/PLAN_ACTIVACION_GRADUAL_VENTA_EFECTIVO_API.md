# Plan activacion gradual venta efectivo API

## Estado actual

La venta en efectivo WPF por API quedo integrada y validada en Test.

La escritura permanece apagada por defecto:

- `UseVentasApiEfectivoWrite=false`.
- `EnableVentasApiWrite=false`.
- `EnableVentasApiEfectivoCajaWrite=false`.
- `EnableCajaApiWrite=false`.

## Requisitos antes de ampliar uso

1. Mantener `UseVentasApiWrite=false` hasta definir corte formal de pagos no efectivos.
2. Activar `UseVentasApiEfectivoWrite=true` solo en estaciones piloto.
3. Activar flags de API solo en ventanas controladas o ambiente Test.
4. Validar que cada estacion use login API y permisos correctos.
5. Mantener `UseCajaApiRead=true` para visibilidad operativa.
6. No activar produccion hasta cerrar plan de reversas inmutables.

## Pruebas recomendadas siguientes

- Venta efectiva WPF con API caida antes de confirmar, sin crear venta.
- Venta efectiva con inventario insuficiente usando prueba no destructiva.
- Venta efectiva sin permiso `Ventas.Crear`.
- Reintento idempotente simulado por timeout.
- Validacion de reporteria posterior con ventas API + caja.

## Fuera de alcance todavia

- Tarjeta.
- SINPE.
- Saldo cliente.
- Dolares.
- Donaciones.
- Pagos combinados.
- Reversas.
- Produccion.

## Recomendacion

El siguiente modulo debe enfocarse en lectura/reporteria de ventas y caja despues de ventas API, o en el diseno de reversas inmutables antes de cualquier corte productivo.
