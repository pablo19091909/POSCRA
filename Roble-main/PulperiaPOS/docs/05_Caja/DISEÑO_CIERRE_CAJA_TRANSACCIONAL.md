# Diseno cierre de caja transaccional

## Objetivo

El cierre futuro debe cerrar un turno, no un dia calendario. Debe calcular efectivo esperado desde `movimiento_caja`, capturar efectivo contado, registrar diferencia y bloquear movimientos posteriores.

## Flujo propuesto

```text
POST /api/caja/turnos/{id}/pre-cierre
-> validar turno Abierto
-> cambiar a EnCierre
-> calcular efectivo esperado
-> devolver resumen seguro

POST /api/caja/turnos/{id}/cerrar
-> validar turno EnCierre
-> recibir efectivo contado
-> calcular diferencia
-> exigir observacion si hay diferencia fuera de tolerancia
-> registrar datos de cierre
-> cambiar a Cerrado
-> auditar
```

## Calculo esperado

```text
Fondo inicial
+ VentaEfectivo
+ IngresoCaja
- RetiroCaja
+ AjustePositivo
- AjusteNegativo
- DevolucionEfectivo futura
```

El calculo debe usar `movimiento_caja` confirmado por `idTurno`.

## Tolerancias y autorizacion

- Tolerancia inicial recomendada: configurable, por defecto cero.
- Si hay diferencia distinta de cero, exigir observacion.
- Diferencias fuera de tolerancia requieren permiso futuro `Caja.CerrarConDiferencia` o autorizacion administrativa.
- Cierre debe registrar usuario que cierra.

## Concurrencia

- Al iniciar pre-cierre, el turno pasa a `EnCierre`.
- Ventas API deben rechazar turnos `EnCierre`.
- La transicion debe ejecutarse con transaccion SQL.
- `row_version` evita doble cierre desde dos ventanas.

## Transicion con SQL historico

Durante la transicion:

- Ventas SQL historicas siguen fuera de `movimiento_caja` hasta migracion posterior.
- Ventas API nuevas pueden exigir turno abierto cuando se active la fase de integracion.
- `cierre_caja` historico puede mantenerse como reporte legado.
- No se debe cerrar por fecha de calendario; se debe cerrar por turno y estado.

## Comparacion con `cierre_caja`

`cierre_caja` actual registra totales por metodo y observaciones, pero no guarda esperado/contado/diferencia formal ni turno. El nuevo cierre debe poder convivir temporalmente sin alterar esos registros.
