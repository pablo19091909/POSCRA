# Diseno de idempotencia de ventas

Fecha UTC: 2026-06-26 10:54:47 UTC

## Objetivo

Evitar que doble clic, reintento de red o repeticion del mismo request cree ventas duplicadas.

## Tabla propuesta

`venta_idempotencia`

Campos principales:

- `idIdempotencia`: PK tecnica.
- `idempotency_key`: `uniqueidentifier`, generado por WPF por intento de venta.
- `usuario_id`: FK a `usuario`.
- `request_hash`: hash seguro del payload canonico.
- `estado`: `EnProceso`, `Completada`, `Fallida`.
- `factura`: FK nullable a `ventas`.
- `response_hash`: hash opcional de respuesta canonica.
- `error_code`: codigo seguro si falla.
- `creado_utc`, `actualizado_utc`, `expira_utc`.
- `observaciones`: texto tecnico no sensible.

## Indices y constraints

- PK: `idIdempotencia`.
- Unico: `(usuario_id, idempotency_key)`.
- FK: `usuario_id -> usuario.idUsuario`.
- FK: `factura -> ventas.factura`.
- CHECK de estado.
- CHECK de expiracion posterior a creacion.

## Comportamiento futuro

```text
Recibir request
-> calcular request_hash
-> intentar insertar clave EnProceso
-> si inserta: continuar venta transaccional
-> si ya existe misma key:
   -> mismo usuario + mismo hash + Completada: devolver misma respuesta segura
   -> mismo usuario + mismo hash + EnProceso reciente: responder solicitud en proceso
   -> mismo usuario + mismo hash + Fallida expirada: permitir reintento controlado
   -> mismo usuario + hash distinto: rechazar reutilizacion de clave
```

## Solicitud EnProceso colgada

Si una solicitud queda `EnProceso` por caida:

- usar `actualizado_utc` y `expira_utc`;
- permitir reintento solo cuando la politica de expiracion lo autorice;
- registrar auditoria `ErrorDeProcesamiento` si se marca fallida;
- no crear una segunda venta sin resolver la clave anterior.

## Limpieza futura

Politica sugerida:

- mantener completadas por periodo operacional definido;
- limpiar fallidas expiradas sin `factura`;
- nunca borrar claves relacionadas con ventas sin auditoria previa.

## Datos sensibles

No almacenar payload completo, tokens, contrasenas, connection strings ni datos personales. Usar hash del request canonico cuando sea suficiente.
