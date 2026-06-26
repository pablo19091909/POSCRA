# Politica de manejo de errores API en WPF

## Principio

Los errores de API deben mostrarse al usuario con mensajes seguros y accionables, sin revelar detalles internos.

Nunca registrar ni mostrar:

- JWT;
- header `Authorization`;
- contrasenas;
- connection strings;
- request body de login;
- hashes;
- stack traces;
- servidor, base de datos o usuario SQL.

## Respuestas HTTP

| Caso | Mensaje/accion |
|---|---|
| 400 | La solicitud no pudo ser procesada. |
| 401 | Limpiar sesion y volver a login. |
| 403 | No tiene permiso para realizar esta accion. |
| 429 | Se alcanzo el limite de intentos. Esperar e intentar nuevamente. |
| Timeout/red | No fue posible comunicarse con el servicio. Verificar conexion e intentar nuevamente. |
| 500/503 | El servicio no esta disponible temporalmente. Intentar mas tarde. |

## Metadata permitida

Se permite conservar metadata no sensible:

- tipo de error;
- endpoint logico;
- codigo HTTP;
- `traceId` si la API lo entrega.

No se debe guardar el cuerpo completo de errores si puede contener informacion sensible.

## Fallback

No existe fallback automatico de API hacia SQL.

Motivo:

- evita saltarse JWT o permisos;
- evita ejecutar operaciones con una sesion vencida;
- mantiene trazabilidad clara durante la migracion gradual.

## Permisos

Los permisos en WPF son solo UX:

- ocultar botones;
- deshabilitar opciones;
- mejorar la experiencia.

La autoridad final debe ser `POS.Api` cuando cada modulo sea migrado.

## Modulos fuera de alcance

En esta fase no cambian:

- ventas;
- inventario;
- clientes;
- caja;
- cierres;
- reportes;
- donaciones;
- tipo de cambio;
- usuarios.

Estos modulos siguen usando SQL directo hasta fases futuras.
