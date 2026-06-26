# Control de acceso - Selector de cliente en ventas

Fecha UTC: 2026-06-26 02:58:37 UTC

## Superficie protegida

Cuando `VentasPage` usa `POS.Api` para cargar o buscar clientes, la operacion consume `GET /api/clientes`.

## Permiso requerido

El endpoint requiere el permiso:

```text
Clientes.Ver
```

## Resultado esperado por escenario

- Token valido con `Clientes.Ver`: HTTP 200.
- Token ausente: HTTP 401.
- Token valido sin `Clientes.Ver`: HTTP 403.
- Error interno o de conectividad: respuesta segura sin secretos.

## Validacion ejecutada

- Acceso con permiso `Clientes.Ver`: confirmado HTTP 200.
- Acceso sin token: confirmado HTTP 401.
- Acceso con token sin permiso requerido: confirmado HTTP 403.

No se imprimieron ni documentaron tokens, usuarios, passwords, hashes, signing keys ni cadenas de conexion.

## Limites de la fase

Esta fase protege solo la consulta del selector de cliente cuando el feature flag esta activo. La logica transaccional de ventas continua en WPF con SQL directo y no fue modificada.

## Riesgos pendientes

- Confirmar visualmente con operador que el usuario real conserva el permiso `Clientes.Ver`.
- Mantener el flag desactivado por defecto hasta completar validacion operativa.
- Migrar otros selectores y consultas de ventas de forma gradual, sin mezclar escrituras en esta etapa.
