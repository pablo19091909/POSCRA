# Resultados reversa efectivo WPF Caja API

## Estado actual

No se ejecuto reversa real. La integracion quedo lista y bloqueada por flags.

## Endpoint

`POST /api/ventas/{factura}/reversas`

Resultados con flags apagados:

- sin token: 401;
- sin permiso: 403;
- con permiso y flags apagados: 503.

## WPF

Se agrego `Modo Reversa API` desde historial de ventas, con:

- validacion de flag;
- permiso `Ventas.Reversar`;
- elegibilidad de venta API efectivo;
- razon obligatoria;
- confirmacion irreversible;
- bloqueo de controles durante envio;
- bloqueo de borrado fisico para venta API efectivo cuando el modo reversa esta activo.

## Pendiente

Prueba manual real desde WPF:

1. abrir turno;
2. crear venta efectivo;
3. reversar la venta;
4. cerrar exacto.
