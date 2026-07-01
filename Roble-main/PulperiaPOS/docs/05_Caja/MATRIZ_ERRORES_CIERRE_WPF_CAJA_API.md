# Matriz de errores - Cierre WPF Caja API

Fecha UTC: 2026-06-30

## Mensajes seguros

- Sin turno abierto: `No hay un turno abierto para cerrar.`
- Turno cerrado o no abierto: `La informacion del turno cambio. Actualice la pantalla antes de cerrar.`
- Sin permiso: `No tiene permiso para cerrar turnos de caja.`
- Sesion expirada: `Su sesion ha vencido. Inicie sesion nuevamente.`
- Efectivo invalido: `El efectivo contado debe ser un valor valido.`
- Diferencia sin observacion: `Debe indicar una observacion para justificar la diferencia de cierre.`
- Timeout: `No fue posible confirmar el resultado del cierre. Revise la conexion y reintente sin cambiar los datos.`
- API no disponible: `No fue posible comunicarse con el servicio de caja. Intente nuevamente.`
- Version desactualizada: `La informacion del turno cambio. Actualice la pantalla antes de cerrar.`

## Mapeo tecnico

- `400`: solicitud no valida.
- `401`: sesion vencida o token invalido.
- `403`: permiso insuficiente.
- `404`: turno no encontrado.
- `409`: conflicto de turno, `rowVersion` o idempotencia.
- `429`: limite de solicitudes.
- `503`: API no disponible o escritura apagada.

## Prohibiciones

Los errores no deben mostrar:

- Stack trace.
- Endpoint.
- SQL.
- Host o puerto.
- Tokens.
- Idempotency keys.
- Hash.
- `rowVersion`.
- IDs internos.
- Datos personales.
