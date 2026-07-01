# Validacion lectura Caja API y no fallback de retiro WPF

Fecha UTC: 2026-06-30

## Configuracion

- Lectura Caja API activa.
- Escritura retiro Caja API apagada.
- Escritura general Caja API apagada.
- Ventas API apagadas.
- `Environment=Test`.

## Ruta de lectura

La pantalla de retiros consulta informacion de turno y resumen de caja mediante Caja API cuando la lectura esta activa.

Resultado validado:

- Turno abierto mostrado.
- Fondo inicial mostrado.
- Resumen financiero mostrado.
- Efectivo esperado mostrado.

## Ruta de escritura

Con `UseCajaApiRetiroWrite=false`, la pantalla conserva el modo historico SQL para registrar retiros.

Durante esta fase no se presiono el boton de registro, por lo tanto no hubo escritura historica ni escritura API.

## No llamada a retiro API

Se confirmo por configuracion y ruta de codigo que `RegistrarRetiroAsync` no debe ejecutarse mientras `UseCajaApiRetiroWrite=false`.

Tambien se confirmo operativamente que:

- No se creo movimiento de retiro.
- No se creo idempotencia nueva.
- No se modifico el turno.

## API no disponible

Se detuvo temporalmente POS.Api y el operador valido que la pantalla muestra manejo seguro de indisponibilidad.

La prueba no ejecuto registro de retiro ni escritura en base de datos.

## Limitaciones

No se probo visualmente en esta fase:

- Usuario sin permiso `Caja.Ver`.
- Sesion expirada.
- Escritura de retiro por API.
- Idempotencia real de retiro desde WPF.

Estas validaciones pertenecen a una fase posterior.
