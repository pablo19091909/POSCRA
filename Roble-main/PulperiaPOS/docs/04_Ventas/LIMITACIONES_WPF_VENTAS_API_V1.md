# Limitaciones WPF Ventas API V1

## Soportado

- Efectivo en colones.
- Tarjeta con voucher.
- Sinpe con referencia.
- Saldo Cliente.
- Un solo pago por venta.
- Recibo solo despues de exito confirmado.
- No fallback automatico a SQL.

## No soportado todavia

- Dolares.
- Donacion.
- Pagos combinados.
- CajaTurno.
- MovimientoCaja.
- Activacion por defecto.
- Ventas reales fuera de ambiente Test.

## Autoridad de datos

En ruta API, la API es autoridad de:

- precio unitario;
- subtotal;
- total;
- stock;
- saldo;
- vuelto;
- usuario;
- factura;
- fecha/hora oficial;
- estado financiero.

WPF solamente envia intencion de venta.

## Pendiente para siguientes fases

- Prueba manual completa WPF con venta sintetica en Test.
- Validacion de permisos por rol desde WPF.
- Pruebas de reintento real.
- Decision funcional sobre apertura de gaveta en ruta API.
- Integracion futura con caja cuando existan contratos transaccionales.
