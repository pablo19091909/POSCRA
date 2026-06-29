# Fase 4F.1 - Auditoria y diseno tecnico de caja API

Fecha/hora de cierre: 2026-06-28 11:08 UTC.

## Alcance

Esta fase fue solo de analisis, diseno y preparacion de scripts no ejecutados. No se modifico codigo funcional, no se activo escritura API y no se ejecuto ningun script SQL 008.

## Flujo actual encontrado

| Flujo | Archivo/metodo | Tablas | Observacion |
| --- | --- | --- | --- |
| Ingreso manual | `IngresoCajaPage.xaml.cs` / `RegistrarIngreso_Click` | `ingreso_caja` | Inserta monto, motivo, fecha, hora y usuario como texto. Usa `DateTime.Now` del cliente. |
| Retiro manual | `Views/RetirosCajaPage.xaml.cs` / `RegistrarRetiro_Click` | `retiro_caja` | Valida disponible en UI con agregado previo, abre gaveta antes del insert y no guarda usuario en tabla. |
| Cierre | `Views/CierreCajaPage.xaml.cs` / `GuardarCierre_Click` | `cierre_caja` | Inserta totales calculados; no guarda turno, caja, usuario responsable, efectivo contado ni diferencia. |
| Calculo caja | `CajaHelper.ObtenerTotalesCaja` | `ventas`, `ingreso_caja`, `retiro_caja`, `cierre_caja`, `cliente` | Suma por fecha del cliente y hora posterior al ultimo cierre del dia. |
| Disponible acumulado | `CajaHelper.ObtenerDineroAcumuladoCajaChica` | `ventas`, `ingreso_caja`, `retiro_caja` | Suma historica de todas las ventas efectivo + ingresos - retiros. |
| Venta SQL | `VentasPage.PagarConSql` | `ventas`, `DetalleVenta`, `inventario`, `cliente` | Inserta venta y afecta stock/saldo; abre gaveta si aplica. No crea movimiento de caja formal. |
| Venta API | `POS.Api` / `VentaRepository.CreateVentaTransactionalAsync` | `ventas`, `DetalleVenta`, `venta_pago`, `venta_auditoria`, `venta_idempotencia` | Transaccional para venta, pago, auditoria e idempotencia; aun no inserta movimiento de caja. |

## Calculo actual de efectivo

El sistema actual estima efectivo disponible como:

```text
ventas en efectivo + ingresos de caja - retiros de caja
```

Para cierre del dia, `CajaHelper.ObtenerTotalesCaja` usa fecha local y, si existe cierre previo del mismo dia, filtra por `hora > ultima_hora_cierre`. Esto no representa un turno real.

## Regla futura propuesta

```text
Efectivo esperado =
Fondo inicial
+ ventas en efectivo netas
+ ingresos de caja
- retiros de caja
+ ajustes positivos autorizados
- ajustes negativos autorizados
- devoluciones/reembolsos en efectivo futuros
```

Notas:

- Venta en efectivo aporta solo el total neto de venta.
- `monto_recibido` y `vuelto` son auditoria de pago, no doble efecto de caja.
- Tarjeta, SINPE y Saldo Cliente no incrementan efectivo fisico.
- Dolares queda fuera de alcance hasta definir moneda/cambio de caja.
- Donacion requiere tratamiento propio.

## Riesgos principales

- Caja no tiene fuente de verdad inmutable por movimiento.
- El cierre se basa en agregados por fecha/hora, no por turno.
- Retiros validan disponible fuera de una transaccion.
- No existe bloqueo de ventas concurrentes durante cierre.
- No hay diferencia formal entre efectivo esperado y contado.
- No hay usuario FK en retiro ni cierre.
- Ventas SQL historicas y ventas API conviven sin modelo unico de caja.

## Diseno futuro resumido

Se propone agregar `caja_turno` y `movimiento_caja` de forma aditiva. `movimiento_caja` sera la fuente de verdad del efectivo fisico nuevo, sin backfill inicial sobre historicos.

Flujo futuro para venta API en efectivo:

```text
Validar turno abierto
-> registrar venta
-> registrar detalle
-> registrar pago
-> descontar stock/saldo si aplica
-> insertar movimiento_caja tipo VentaEfectivo por total neto
-> auditar
-> commit o rollback total
```

## Validacion final

- Scripts 008 preparados, no ejecutados.
- Documentacion de diseno creada.
- Feature flags de ventas API permanecen apagados.
- No se altero caja, ventas, inventario, saldo, pagos ni datos historicos.
