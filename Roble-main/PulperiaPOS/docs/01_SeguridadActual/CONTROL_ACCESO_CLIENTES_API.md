# Control de acceso Clientes API

## Endpoint

```http
GET /api/clientes
```

## Permiso requerido

`Clientes.Ver`

El permiso ya existia en la matriz:

- `Administrador`: incluido.
- `Anfitrion`: incluido.

No se agregaron permisos nuevos.

## Comportamiento de seguridad

| Caso | Resultado |
|---|---|
| Sin token | HTTP 401 |
| Token sin `Clientes.Ver` | HTTP 403 |
| Token con `Clientes.Ver` | HTTP 200 |
| `limit` invalido | HTTP 400 |

## Datos expuestos

Solo se exponen campos ya visibles o necesarios para listado:

- id;
- nombre;
- saldo;
- comprobante;
- fecha de carga de saldo.

No se exponen:

- contrasenas;
- hashes;
- usuarios;
- JWT;
- connection strings;
- historial de ventas;
- detalles internos SQL.

## SQL Injection

La busqueda usa parametros SQL.

Se probo busqueda con caracteres especiales y respondio de forma segura.

## Sin fallback automatico

Si `UseClientesApi=true` y falla la API:

- WPF muestra error seguro;
- no consulta SQL automaticamente;
- no modifica datos.

Esto evita saltarse JWT o permisos.

## Limitaciones

La tabla `cliente` no tiene columna `activo`; por ahora no existe filtro real de clientes activos/inactivos.

## Recomendacion

Mantener esta API en solo lectura hasta completar validacion visual de `ClientePage` y luego migrar un segundo consumo de lectura, preferiblemente selector de cliente de ventas.
