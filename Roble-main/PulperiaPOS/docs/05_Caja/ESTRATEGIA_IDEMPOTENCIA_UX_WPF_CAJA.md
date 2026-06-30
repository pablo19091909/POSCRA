# Estrategia de idempotencia UX WPF Caja

Fecha/hora UTC: 2026-06-30 02:13:13 UTC

## Objetivo

Evitar dobles ejecuciones por doble clic, timeouts o reintentos mientras se integran futuras escrituras de caja por API.

## Componente preparado

`CajaOperationCoordinator` mantiene una operacion pendiente por intencion de usuario.

Cada `PendingCajaOperation` contiene:

- nombre logico de operacion;
- huella de intencion;
- `Idempotency-Key` GUID;
- estado local: `Ready`, `InProgress`, `ResultUncertain`.

La llave no debe mostrarse ni registrarse.

## Reglas de uso futuro

Antes de enviar escritura API:

- construir una huella canonica de intencion;
- llamar a `GetOrCreate`;
- llamar a `TryBegin`;
- si ya esta `InProgress`, no enviar segunda solicitud;
- deshabilitar temporalmente el boton de UI;
- conservar la pantalla.

Si API confirma exito:

- limpiar la operacion pendiente;
- refrescar datos desde API;
- no ejecutar SQL historico.

Si API devuelve error de negocio:

- mostrar mensaje seguro;
- marcar listo para reintento si la intencion sigue siendo la misma;
- no generar una llave nueva sin decision explicita del usuario.

Si ocurre timeout o red caida:

- marcar resultado incierto;
- informar que no se pudo confirmar el resultado;
- permitir reintento con la misma intencion y la misma llave;
- no ejecutar fallback SQL.

## Doble clic

El doble clic debe bloquearse localmente mediante `TryBegin`.

Una segunda solicitud no debe salir mientras la primera esta en progreso.

## Limites de esta fase

El coordinador no fue conectado a botones de escritura.

No se generaron idempotencias en base de datos durante esta fase.

