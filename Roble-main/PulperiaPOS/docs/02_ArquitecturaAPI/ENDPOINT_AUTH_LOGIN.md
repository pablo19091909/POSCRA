# Endpoint POST /api/auth/login

## Estado

Endpoint preparado en Fase 3B.1.

No reemplaza aun el login WPF actual.

## Ruta

`POST /api/auth/login`

## Rate limit

Este endpoint usa la politica `AuthLogin`.

Por defecto:

- 5 solicitudes;
- 60 segundos;
- respuesta 429 al exceder limite.

## Request

```json
{
  "username": "texto",
  "password": "texto"
}
```

## Respuesta exitosa

```json
{
  "accessToken": "jwt",
  "expiresAtUtc": "fecha UTC",
  "user": {
    "id": 0,
    "username": "texto",
    "role": "Administrador",
    "permissions": [
      "Ventas.Crear"
    ]
  }
}
```

No se retornan hashes, contrasenas ni datos personales innecesarios.

## Respuestas seguras

| Caso | HTTP | Mensaje |
| --- | --- | --- |
| Request vacio o invalido | 400 | Solicitud invalida |
| Usuario inexistente | 401 | Credenciales invalidas |
| Contrasena incorrecta | 401 | Credenciales invalidas |
| Usuario inactivo | 401 | Credenciales invalidas |
| Usuario bloqueado | 401 | Credenciales invalidas |
| JWT no configurado | 503 | Autenticacion no disponible |
| Rate limit excedido | 429 | Respuesta estandar del middleware |

No se diferencia publicamente entre usuario inexistente, password incorrecto, usuario inactivo o bloqueo temporal.

## Antes de migracion SQL

El endpoint puede validar usuarios con SHA-256 legado si las columnas modernas aun no existen.

Con `EnableLegacyHashUpgrade=false`, no actualiza ningun hash.

## Despues de migracion SQL

El endpoint puede:

- validar `password_hash_v2` con BCrypt;
- validar SHA-256 legado si `password_hash_v2` esta vacio;
- migrar hash legado a BCrypt solo cuando `EnableLegacyHashUpgrade=true`.

## JWT

El token incluye:

- `userId`;
- `username`;
- `role`;
- claims `permission`;
- issuer;
- audience;
- expiracion.

Los endpoints publicos permanecen sin autenticacion:

- `/health`
- `/health/database`
- `/api/system/version`
