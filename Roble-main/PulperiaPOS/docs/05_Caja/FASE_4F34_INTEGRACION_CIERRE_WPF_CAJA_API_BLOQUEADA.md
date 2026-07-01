# FASE 4F.34 - Integracion bloqueada CierreCajaPage con Caja API

Fecha UTC: 2026-06-30

## Alcance

Se audito e integro una ruta bloqueada de cierre API en `CierreCajaPage`.

No se activo `UseCajaApiCierreWrite`. No se cerro el turno actual. No se ejecutaron escrituras de caja, ventas, pagos, inventario, cliente ni tablas historicas.

## Auditoria de flujo historico

Con el flag API apagado, `CierreCajaPage` conserva su comportamiento historico:

- Calcula totales usando `CajaHelper`.
- Consulta ventas del dia con SQL directo.
- Lee cierres anteriores desde `cierre_caja`.
- Abre gaveta con `RawPrinterHelper`.
- Inserta en `cierre_caja`.
- Imprime cierre historico.
- Usa fecha/hora local.

Tablas historicas tocadas por el flujo historico cuando se ejecuta:

- `cierre_caja`.
- `ventas` para calculos.

Riesgos del flujo historico frente al API:

- Sin idempotencia.
- Sin `rowVersion`.
- Calculo fuera de transaccion de cierre.
- Riesgo de doble clic.
- Fecha/hora local.
- Impresion acoplada a persistencia.
- No controla estado `Abierto -> EnCierre -> Cerrado`.

## Integracion preparada

Se agregaron modelos WPF:

- `CajaCierreRequest`.
- `CajaCierreResult`.
- `CajaCierreViewModel`.
- `CierreCajaApiResponse`.

Se agrego `CajaApiClient.CerrarTurnoAsync(...)`.

Se preparo `CierreCajaPage` con bifurcacion explicita:

- `UseCajaApiCierreWrite=false`: flujo historico intacto.
- `UseCajaApiCierreWrite=true`: ruta preparada para Caja API.

## Ruta API preparada

La ruta API obtiene desde Caja API:

- Turno abierto.
- Pre-cierre.
- Efectivo esperado.
- `rowVersion`.
- Resumen de movimientos permitido.

La ruta API envia:

- `efectivoContado`.
- `observacion`.
- `rowVersion`.
- `Idempotency-Key` solo por header.

La ruta API no envia:

- Usuario.
- Fecha.
- Esperado.
- Diferencia final.
- Estado.
- Movimiento.
- Hash.
- UTC.
- Identificador de cierre.

## Protecciones

- Usa `decimal`.
- Valida efectivo contado no negativo.
- Valida `rowVersion` Base64 no vacio y de 8 bytes.
- Exige observacion si la diferencia visual estimada es distinta de cero.
- Usa `CajaOperationCoordinator`.
- Bloquea boton, campos y navegacion de cierre durante operacion API.
- Conserva la misma key para la misma intencion pendiente.
- No usa fallback SQL en ruta API.
- No usa `CajaHelper`, `DBConnection` ni `RawPrinterHelper` en ruta API.

## Validacion no destructiva

Con `UseCajaApiCierreWrite=false`:

- No se llamo endpoint de cierre.
- No se genero idempotency key de cierre WPF.
- No se creo movimiento `CierreDiferencia`.
- No se creo idempotencia nueva `CerrarTurno`.
- No cambio el turno abierto.
- No cambio efectivo esperado.
- No se escribio en `cierre_caja`.

## Integridad confirmada

Lecturas agregadas:

- Turno abierto `CAJA_PRINCIPAL_TEST`: 1.
- Turnos `EnCierre`: 0.
- `CierreDiferencia` en turno abierto: 0.
- `ingreso_caja`: 9.
- `retiro_caja`: 6.
- `cierre_caja`: 15.
- `ventas`: 1948.
- `venta_pago`: 10.
- `venta_idempotencia`: 10.
- Stock agregado: 3296.00.
- Saldo agregado clientes: -2957962.50.
- Fondo inicial: 1000.00.
- Ingresos: 100.00.
- Retiros: 100.00.
- Efectivo esperado: 1000.00.

Nota: existen idempotencias `CerrarTurno` de fases previas asociadas a turnos ya cerrados; no se creo ninguna nueva en esta fase.

## Compilacion

- WPF: 0 errores; advertencias heredadas existentes.
- POS.Api: 0 errores, 0 advertencias.

## Riesgos pendientes

- No se ha ejecutado cierre WPF por API.
- No se probo usuario sin `Caja.Cerrar`.
- No existen reversas.
- Ventas API en efectivo aun no estan integradas a Caja API.
- Dolares, Donacion y pagos combinados no estan integrados a Caja API.

## Recomendacion

Continuar con Fase 4F.35: validacion visual no destructiva de `CierreCajaPage` en modo API bloqueado, con escritura API apagada y sin cerrar el turno.
