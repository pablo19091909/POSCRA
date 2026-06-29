# Estrategia de idempotencia para operaciones de caja

## Estado actual

No existe idempotencia persistente especifica para caja. En Fase 4F.7 no se crearon tablas ni migraciones.

## Riesgo

Un ingreso no puede depender solo de bloquear doble clic en UI. Reintentos de red, timeouts, doble envio o recuperaciones manuales pueden crear duplicados si no existe una llave tecnica persistente.

## Diseno recomendado

Antes de habilitar ingresos repetibles desde WPF o clientes externos, crear una tabla o mecanismo equivalente con:

- llave de idempotencia;
- hash normalizado del request;
- usuario;
- operacion;
- caja logica;
- estado de procesamiento;
- resultado seguro;
- relacion con `idMovimiento`;
- fechas UTC de creacion y finalizacion.

## Operaciones que deben usarla

- ingreso;
- retiro;
- cierre;
- ajuste;
- reversa.

## Apertura 4F.6

La apertura controlada de Fase 4F.6 fue una excepcion operativa manual con una unica intencion autorizada. No es un patron suficiente para produccion.

## Recomendacion

Definir migracion de idempotencia de caja antes de conectar WPF o permitir reintentos automaticos.
