# Resultados idempotencia y UX ingreso WPF

Fecha UTC: 2026-06-30 14:13:07 UTC

## UX observada

Validacion manual confirmada por operador:

- `IngresoCajaPage` mostro modo Caja API.
- El turno abierto fue detectado desde API.
- El efectivo esperado previo fue visible desde API.
- Los campos disponibles fueron monto, motivo y referencia opcional.
- No se mostraron key, hash, rowVersion ni identificadores tecnicos.
- El ingreso no se ejecuto automaticamente al abrir la pantalla.
- Durante el envio, boton y campos se bloquearon.
- La ventana no se congelo.
- El mensaje de exito fue claro y no tecnico.
- El formulario se limpio despues de recibir confirmacion exitosa.
- El estado de turno se refresco desde API.

## Doble clic e idempotencia

La observacion de UI y los agregados posteriores confirmaron:

- se creo un solo movimiento `IngresoCaja`;
- se creo una sola idempotencia `IngresoCaja Completada`;
- no quedo idempotencia `EnProceso`;
- no hubo duplicidad de movimiento ni de idempotencia;
- no se hizo una segunda operacion manual despues del exito.

## Conteos relevantes

| Metrica | Antes | Despues | Resultado |
| --- | ---: | ---: | --- |
| `movimiento_caja` | 10 | 11 | +1 esperado |
| `caja_idempotencia` | 8 | 9 | +1 esperado |
| `IngresoCaja` API | 2 | 3 | +1 esperado |
| Idempotencia `IngresoCaja Completada` | 2 | 3 | +1 esperado |
| Idempotencias `EnProceso` | 0 | 0 | Sin pendientes |
| Efectivo esperado | 1000.00 | 1100.00 | +100.00 esperado |

## Limitaciones

No se imprimio comprobante. No se probo reintento despues de timeout porque el resultado fue exitoso y la fase prohibia generar un segundo ingreso.
