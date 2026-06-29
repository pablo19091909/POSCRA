# Hallazgo externo de saldos pendiente de auditoria

## Hallazgo

Durante las pruebas de caja se observo un saldo agregado de clientes con valor negativo considerable.

Valor agregado observado en Fase 4F.7:

```text
cliente_saldo_centavos_sum = -295796250
```

## Alcance

Este hallazgo:

- no fue generado por Caja API;
- no fue generado por la lectura del turno;
- no fue generado por el bloqueo de ingreso;
- no se corrigio ni modifico en esta fase.

## Riesgo

Antes de reactivar ventas API con `SaldoCliente`, conviene ejecutar una auditoria independiente de solo lectura para entender origen, antiguedad, clientes afectados, operaciones relacionadas y regla de negocio esperada.

## Recomendacion

Crear una fase separada de auditoria de saldos de clientes, estrictamente de solo lectura, sin correcciones automaticas ni modificaciones de datos.
