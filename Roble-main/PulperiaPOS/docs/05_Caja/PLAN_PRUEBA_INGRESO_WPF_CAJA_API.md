# Plan prueba - Ingreso WPF Caja API

Fecha/hora UTC: 2026-06-30T13:45:44Z

## Precondiciones

- `Environment=Test`.
- `writes_allowed_for_testing=1`.
- Un turno abierto en `CAJA_PRINCIPAL_TEST`.
- `UseCajaApiRead=true`.
- `UseCajaApiIngresoWrite=false` hasta fase autorizada.
- `EnableCajaApiWrite=false` hasta fase autorizada.

## Pruebas ya ejecutadas

| Prueba | Resultado |
| --- | --- |
| Sin token | 401 |
| Sin permiso | 403 |
| Con permiso y escritura apagada sin key | 503 |
| Con permiso y escritura apagada key invalida | 503 |
| Con permiso y escritura apagada key valida | 503 |
| Conteos de caja | Sin cambios |
| `ingreso_caja` historico | Sin cambios |

## Pruebas puras pendientes en UI

- monto valido positivo;
- monto cero invalido;
- monto negativo invalido;
- motivo vacio invalido;
- referencia valida;
- referencia demasiado larga invalida;
- doble clic bloqueado;
- timeout conserva formulario y key;
- error 503 no usa SQL;
- error 409 no usa SQL.

## Primera prueba destructiva futura

Solo en Fase 4F.28, con autorizacion explicita:

1. Activar temporalmente `UseCajaApiIngresoWrite=true`.
2. Activar temporalmente `EnableCajaApiWrite=true`.
3. Registrar un unico ingreso sintetico pequeño.
4. Reintentar con misma intencion para validar idempotencia.
5. Confirmar un solo movimiento `IngresoCaja` y una sola idempotencia completada.
6. Restaurar flags a `false`.

No probar retiros ni cierres en esta fase futura.
