# Feature flags de reversas

## WPF

`UseVentasApiReversaWrite`

- Valor actual: `false`.
- Proposito: habilitar UI futura de reversa desde WPF.
- Estado: no conectado a botones finales en esta fase.

## API

`EnableVentasApiReversaCajaWrite`

- Valor actual: `false`.
- Proposito: habilitar reversa de venta efectivo integrada con Caja API.
- Estado: bloquea el endpoint con HTTP 503 seguro.

## Compuerta completa futura

Una reversa real debe requerir simultaneamente:

- `UseVentasApiReversaWrite=true`.
- `EnableVentasApiWrite=true`.
- `EnableCajaApiWrite=true`.
- `EnableVentasApiReversaCajaWrite=true`.
- Ambiente de base `Test`.
- JWT valido.
- Permiso `Ventas.Reversar`.
- Venta elegible.
- Turno abierto.
- Idempotency key valida.

## Resultado de esta fase

Los flags fueron agregados y dejados apagados en archivos versionados y configuracion local, sin mostrar valores sensibles.
