# Fase 4C.3B - Pruebas transaccionales venta API en base Test

Fecha/hora UTC: 2026-06-26

## Confirmacion

El operador confirmo que la base conectada actualmente es ambiente de prueba autorizado para escrituras controladas.

## Marca Test

Se creo y ejecuto `database/test/000_MarcarEntornoTest.sql`.

La base quedo marcada con:

- `Environment=Test`.
- `writes_allowed_for_testing=1`.

## Proteccion API

Se implemento proteccion adicional:

- `EnableVentasApiWrite=true`.
- Marca explicita de base `Environment=Test`.

Si cualquiera falla, `POST /api/ventas` responde 503 seguro.

## Datos sinteticos

Se creo y ejecuto `database/test/001_SeedDatosSinteticosVentasApi.sql`.

Aliases creados o validados:

- Usuarios: `API_TEST_ADMIN_VENTAS`, `API_TEST_SIN_PERMISO`.
- Clientes: `API_TEST_CLIENTE_EFECTIVO`, `API_TEST_CLIENTE_TARJETA`, `API_TEST_CLIENTE_SINPE`, `API_TEST_CLIENTE_SALDO_OK`, `API_TEST_CLIENTE_SALDO_BAJO`.
- Productos: `API_TEST_PROD_STOCK_ALTO`, `API_TEST_PROD_STOCK_UNIDAD`, `API_TEST_PROD_STOCK_CERO`, `API_TEST_PROD_PRECIO_DECIMAL`.

No se imprimieron JWT, connection strings, credenciales ni cuerpos completos de request.

## Configuracion temporal

Se uso `POS.Api/appsettings.Test.json`, ignorado por Git, con escritura habilitada temporalmente.

Al cierre se restauro:

`EnableVentasApiWrite=false`.

## Resultados A-E

| Caso | Resultado |
| --- | --- |
| A - Efectivo exacto | HTTP 200 |
| B - Efectivo con vuelto | HTTP 200 |
| C - Tarjeta | HTTP 200 |
| D - Sinpe | HTTP 200 |
| E - SaldoCliente suficiente | HTTP 200 |

Validado:

- Ventas sinteticas creadas.
- Detalles sinteticos creados.
- Stock descontado.
- `vendido` incrementado.
- Pagos creados.
- Auditoria `VentaCreada` creada.
- Idempotencia `Completada`.
- Caja sin cambios.

## Resultados F-L

| Caso | Resultado |
| --- | --- |
| F - Stock insuficiente | HTTP 400 |
| G - Saldo insuficiente | HTTP 400 |
| H - Efectivo insuficiente | HTTP 400 |
| I - Producto inexistente | HTTP 400 |
| J - Cliente inexistente | HTTP 400 |
| K - Donacion | HTTP 400 |
| L - Dolares | HTTP 400 |

Validado:

- Errores seguros.
- Sin ventas parciales.
- Sin pagos, auditorias ni detalles huerfanos.
- Donacion y Dolares permanecen bloqueados.

## Idempotencia y concurrencia

| Caso | Resultado |
| --- | --- |
| M - Misma key, mismo request | HTTP 200, HTTP 200 |
| N - Misma key, request distinto | HTTP 409 |
| O - Concurrencia ultima unidad | HTTP 200, HTTP 400 |

Validado:

- La repeticion segura no creo segunda venta.
- La key reutilizada con request distinto genero conflicto.
- La concurrencia no dejo stock negativo.

## Integridad final

| Metrica | Resultado |
| --- | ---: |
| Ventas globales | 1889 |
| Detalles globales | 4933 |
| Ventas sinteticas `API_TEST_` | 7 |
| Detalles sinteticos `API_TEST_` | 7 |
| `venta_idempotencia` | 7 |
| `venta_pago` | 7 |
| `venta_auditoria` | 7 |
| Pagos huerfanos | 0 |
| Auditorias huerfanas | 0 |
| Detalles huerfanos | 0 |
| Stock negativo `API_TEST_` | 0 |
| Saldo negativo `API_TEST_` | 0 |
| Ingresos caja | 9 |
| Retiros caja | 6 |
| Cierres caja | 15 |

Stock sintetico final:

| Alias | Stock | Vendido |
| --- | ---: | ---: |
| `API_TEST_PROD_PRECIO_DECIMAL` | 99 | 1 |
| `API_TEST_PROD_STOCK_ALTO` | 94 | 6 |
| `API_TEST_PROD_STOCK_CERO` | 0 | 0 |
| `API_TEST_PROD_STOCK_UNIDAD` | 0 | 1 |

Saldo sintetico final:

| Alias | Saldo |
| --- | ---: |
| `API_TEST_CLIENTE_EFECTIVO` | 0.00 |
| `API_TEST_CLIENTE_SALDO_BAJO` | 1.00 |
| `API_TEST_CLIENTE_SALDO_OK` | 990.00 |
| `API_TEST_CLIENTE_SINPE` | 0.00 |
| `API_TEST_CLIENTE_TARJETA` | 0.00 |

## Estado final

- API detenida.
- Puerto `7046` libre.
- `EnableVentasApiWrite=false`.
- WPF no modificado.
- Datos historicos no usados en pruebas.
- Caja sin cambios.

## Criterio

Aprobado con observaciones.

Observaciones:

- Dolares sigue bloqueado por fuente de tipo de cambio no decimal.
- Donacion sigue fuera de alcance V1.
- Aun falta conectar WPF de forma controlada mediante feature flag.
