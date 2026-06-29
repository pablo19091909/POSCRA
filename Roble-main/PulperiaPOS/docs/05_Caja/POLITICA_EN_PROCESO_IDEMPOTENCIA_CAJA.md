# Politica EnProceso idempotencia Caja

## Politica V1

Un registro `EnProceso` responde:

```text
409 Conflict
```

Mensaje seguro: solicitud en proceso, reintentar mas tarde con la misma key.

## Lo que no se permite

- No crear un segundo movimiento.
- No convertir automaticamente a `Completada`.
- No borrar automaticamente.
- No permitir otra key para la misma intencion sin investigacion.

## EnProceso abandonado

Debe definirse una revision administrativa o de conciliacion antes de automatizar recuperacion.

Evidencia minima futura:

- existencia de movimiento relacionado;
- estado de transaccion confirmada;
- auditoria futura;
- fecha UTC;
- usuario;
- turno;
- caja;
- hash de request.

## Umbral temporal

Se recomienda un umbral configurable antes de investigacion, por ejemplo 15 minutos o mas, pero no se implementa job ni limpieza automatica en esta fase.
