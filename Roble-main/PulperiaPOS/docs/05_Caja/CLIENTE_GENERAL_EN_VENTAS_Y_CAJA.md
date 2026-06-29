# Cliente General en ventas y caja

## Regla actual

Cliente General representa ventas a consumidor no registrado. En la base actual existe como cliente con identificador `0` y debe conservarse compatible con reportes y recibos.

## Comportamiento validado

- WPF permite seleccionar Cliente General.
- POS.Api acepta `ClienteId=0` porque valida como invalido solo `ClienteId < 0`.
- Cliente General no debe usar `SaldoCliente`.
- Cliente General no debe descontar saldo de un cliente real.
- Cliente General debe aparecer en recibo como cliente generico.

## Compatibilidad con FK

Como `ventas.cliente_id` referencia `cliente.idCliente`, `Cliente General` debe existir en `cliente` para que `cliente_id=0` sea valido. No se propone cambiar esta regla en Fase 4F.1.

## Caja

Cliente General no cambia el calculo de caja por si mismo. El efecto de caja depende del metodo de pago:

- Efectivo: crea futuro `MovimientoCaja` tipo `VentaEfectivo` por total neto.
- Tarjeta/SINPE: no crea movimiento de efectivo fisico.
- Saldo Cliente: no permitido para Cliente General.

## Contrato futuro

El contrato API debe documentar expresamente:

- `clienteId=0` esta reservado para Cliente General.
- `clienteId=0` no acepta metodo `SaldoCliente`.
- Si se elimina o altera Cliente General en base, la venta API debe fallar de forma segura.

## Riesgos

- Reportes pueden agrupar muchas ventas bajo un unico cliente generico.
- Un selector mal ordenado puede precargar un cliente historico accidentalmente.
- Una regla incompleta podria permitir `SaldoCliente` con Cliente General.
