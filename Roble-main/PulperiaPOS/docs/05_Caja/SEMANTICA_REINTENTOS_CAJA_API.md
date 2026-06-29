# Semantica de reintentos Caja API

## Primera solicitud

```text
Validar ambiente, flag, permiso y turno
-> registrar idempotencia EnProceso
-> crear MovimientoCaja
-> marcar idempotencia Completada con referencia final
-> commit
```

## Misma key y mismo request completado

No crea segundo movimiento. Devuelve el resultado original seguro.

## Misma key y request distinto

Devuelve conflicto seguro `409`. No crea movimiento.

## Misma key EnProceso

Contrato recomendado: devolver `409` seguro indicando operacion en proceso. Alternativa futura: `202`, solo si el cliente tiene mecanismo claro de consulta.

## Error antes de crear movimiento

La transaccion debe revertir completa. Si se decide persistir `Fallida`, debe hacerse mediante estrategia separada y consistente, sin dejar una operacion ambigua.

## Error despues del commit o timeout del cliente

El reintento con la misma key y mismo request debe devolver el resultado original, sin crear segundo ingreso.

## Concurrencia

Dos solicitudes simultaneas con misma key deben producir como maximo un movimiento. La combinacion unica `(usuario_id, operacion, idempotency_key)` es la barrera principal.

## Cambio de intencion

Si cambia monto, motivo, referencia, caja o turno, se requiere nueva key. Reusar la key con hash distinto produce `409`.
