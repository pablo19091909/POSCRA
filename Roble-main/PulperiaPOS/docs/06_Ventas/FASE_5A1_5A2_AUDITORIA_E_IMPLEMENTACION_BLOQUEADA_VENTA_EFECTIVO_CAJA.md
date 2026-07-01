# Fase 5A.1 + 5A.2 - Venta efectivo Caja API bloqueada

Fecha UTC: 2026-07-01

## Resultado

Se audito el flujo real de Ventas API y Caja API, se diseno la integracion transaccional para futuras ventas API pagadas en efectivo y se implemento el soporte bloqueado por feature flags apagados.

No se activo ningun flujo de escritura. No se crearon ventas, detalles, pagos, auditorias, idempotencias, movimientos `VentaEfectivo`, turnos ni cambios de inventario o saldos.

## Flujo real actual de Ventas API

- Endpoint: `POST /api/ventas`.
- Controlador: `VentasController`.
- Permiso: `Ventas.Crear`.
- Servicio: `VentaService.CrearVentaAsync`.
- Repositorio: `VentaRepository.CreateVentaTransactionalAsync`.
- Contrato: `CrearVentaRequest`.
- Metodo de pago: `PagoVentaRequest.MetodoPago`.
- Efectivo se representa como `MetodoPago = "Efectivo"`.

La venta actual usa una unica `SqlConnection` y una unica `SqlTransaction` con aislamiento `Serializable` para:

- crear `venta_idempotencia` en `EnProceso`;
- validar usuario;
- validar cliente;
- bloquear productos con `UPDLOCK, HOLDLOCK`;
- insertar `ventas`;
- insertar `DetalleVenta`;
- descontar inventario;
- descontar saldo cliente si aplica;
- insertar `venta_pago`;
- insertar `venta_auditoria`;
- completar `venta_idempotencia`.

## Flujo real actual de Caja API

Caja API usa:

- `caja_turno`;
- `movimiento_caja`;
- `caja_idempotencia`.

`VentaEfectivo` ya existe como tipo permitido en `movimiento_caja` y suma al efectivo esperado. El pre-cierre y el cierre incluyen `VentaEfectivo` como aumento de efectivo.

`movimiento_caja` contiene:

- `factura` con FK hacia `ventas.factura`;
- `pago_id` con FK hacia `venta_pago.idPago`;
- indice unico filtrado `UX_movimiento_caja_pago_efectivo` para impedir mas de un `VentaEfectivo` confirmado por el mismo pago.

## Implementacion bloqueada

Se agregaron flags:

- WPF/cliente: `UseVentasApiEfectivoWrite=false`.
- API: `EnableVentasApiEfectivoCajaWrite=false`.

La venta efectiva futura solo queda habilitable si:

```text
UseVentasApiEfectivoWrite=true
EnableVentasApiWrite=true
EnableCajaApiWrite=true
EnableVentasApiEfectivoCajaWrite=true
Environment=Test
turno de caja Abierto
```

En servidor, si el request es efectivo y `EnableCajaApiWrite` o `EnableVentasApiEfectivoCajaWrite` esta apagado, la API responde como no disponible antes de iniciar la transaccion de escritura.

## Cambios de codigo

- `FeatureFlagsOptions`: agrega `EnableVentasApiEfectivoCajaWrite`.
- `FeatureFlags`: agrega `UseVentasApiEfectivoWrite`.
- `VentaService`: agrega barrera de flags para efectivo.
- `CrearVentaPreparedCommand`: transporta `IntegrarCajaEfectivo`.
- `VentaRepository`: preparado para bloquear turno abierto y crear `VentaEfectivo` dentro de la misma transaccion.
- `InsertPagoAsync`: ahora obtiene el `idPago` insertado para enlazar `movimiento_caja`.

## Limitacion tecnica documentada

El contrato actual de venta no recibe caja logica. La implementacion bloqueada usa `CAJA_PRINCIPAL_TEST` como caja Test interna. Antes de produccion o multiples cajas, se debe decidir si el contrato de venta incluira `cajaCodigo` o si el servidor resolvera caja por terminal/sesion.

## Validacion no destructiva

Health checks:

- `/health = 200`
- `/health/database = 200`
- `/api/system/version = 200`

HTTP sin token:

- `POST /api/ventas = 401`

Compilacion:

- POS.Api: correcta, 0 advertencias, 0 errores.
- WPF: correcta, 0 errores, advertencias heredadas.

Integridad antes/despues:

| Agregado | Antes | Despues |
| --- | ---: | ---: |
| `ventas` | 1948 | 1948 |
| `DetalleVenta` | 5087 | 5087 |
| `venta_pago` | 10 | 10 |
| `venta_idempotencia` | 10 | 10 |
| `venta_auditoria` | 10 | 10 |
| `movimiento_caja` | 16 | 16 |
| `VentaEfectivo` | 0 | 0 |
| `caja_idempotencia` | 15 | 15 |
| Inventario agregado | 3,296.00 | 3,296.00 |
| Saldo cliente agregado | -2,957,962.50 | -2,957,962.50 |
| `ingreso_caja` | 9 | 9 |
| `retiro_caja` | 6 | 6 |
| `cierre_caja` | 15 | 15 |

## Estado final

- Todos los flags de escritura permanecen apagados.
- POS.Api fue detenido.
- Puerto local libre confirmado.
