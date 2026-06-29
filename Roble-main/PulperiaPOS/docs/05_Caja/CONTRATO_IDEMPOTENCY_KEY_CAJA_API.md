# Contrato Idempotency-Key Caja API

## Header futuro

```text
Idempotency-Key: <guid>
```

## Reglas

- Requerido para escrituras de caja en fase posterior.
- Formato UUID/Guid.
- No puede ser `Guid.Empty`.
- No se registra en logs.
- Se asocia a usuario, operacion y request hash.
- No puede reutilizarse para una intencion distinta.

## Request hash

Para `IngresoCaja`, el hash debe incluir:

- operacion;
- usuario autenticado;
- caja;
- turno resuelto cuando aplique;
- monto;
- motivo;
- referencia.

El hash se almacena como `varbinary(32)` usando SHA-256 sobre una representacion canonica.

## Respuestas seguras

| Caso | Respuesta |
| --- | --- |
| Header omitido | 400 |
| Header invalido | 400 |
| Key repetida mismo request completado | 200 con resultado repetido seguro |
| Key repetida request distinto | 409 |
| Key en proceso | 409 o 202 segun contrato final |
| Error transaccional | rollback y error seguro |

## No registrar

No registrar token, usuario individual, montos detallados, cuerpo completo de request, connection strings ni la propia key.
