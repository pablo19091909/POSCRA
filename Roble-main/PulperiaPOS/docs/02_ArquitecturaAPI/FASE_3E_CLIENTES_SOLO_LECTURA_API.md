# Fase 3E - Clientes solo lectura desde POS.Api

Fecha UTC: 2026-06-26 01:48:49 UTC

## Objetivo

Se agrego una migracion gradual y reversible para consultar clientes desde `POS.Api` en modo solo lectura.

No se migraron operaciones de escritura. Crear, editar, eliminar, liberar saldo, reportes e historial siguen usando SQL directo en WPF.

## Modelo real encontrado

Tabla: `cliente`

| Columna | Tipo | Nullable | Uso actual |
|---|---|---|---|
| `idCliente` | `int` | No | Llave primaria y seleccion en WPF. |
| `nombre` | `nvarchar` | No | Nombre visible y busqueda. |
| `saldo` | `decimal` | Si | Mostrado en `ClientePage`; usado por liberar saldo y reportes SQL. |
| `comprobante` | `nvarchar` | Si | Mostrado en `ClientePage`. |
| `fecha_carga_saldo` | `datetime` | Si | Expuesto para lectura futura; no usado hoy por WPF. |

No existe columna `activo`. Por eso `soloActivos=true` se acepta como parametro de contrato, pero actualmente no filtra registros.

## Endpoint creado

```http
GET /api/clientes
```

Parametros:

- `soloActivos=true|false`
- `busqueda=texto`
- `limit=numero`
- `offset=numero`

Reglas:

- requiere JWT;
- requiere permiso `Clientes.Ver`;
- usa `Microsoft.Data.SqlClient`;
- usa consulta parametrizada;
- orden deterministico por `nombre`, `idCliente`;
- valida `limit` y `offset`;
- no modifica datos.

## Campos expuestos por API

Contrato: `ClienteListItemResponse`

- `idCliente`: necesario para seleccion y acciones existentes.
- `nombre`: necesario para listado y busqueda.
- `saldo`: ya se muestra en `ClientePage`.
- `comprobante`: ya se muestra en `ClientePage`.
- `fechaCargaSaldoUtc`: existe en tabla y queda disponible solo como lectura.

No se exponen historial de ventas, datos de autenticacion, hashes, contrasenas ni informacion interna.

## Componentes API

- `POS.Api/Controllers/ClientesController.cs`
- `POS.Api/Contracts/Clientes/ClienteListItemResponse.cs`
- `POS.Api/Application/Clientes/ClienteQuery.cs`
- `POS.Api/Application/Clientes/IClienteService.cs`
- `POS.Api/Application/Clientes/ClienteService.cs`
- `POS.Api/Infrastructure/Data/Clientes/IClienteRepository.cs`
- `POS.Api/Infrastructure/Data/Clientes/ClienteRepository.cs`
- `POS.Api/Program.cs`

## Componentes WPF

- `PulperiaPOS/ApiClients/ClientesApiClient.cs`
- `PulperiaPOS/Models/Clientes/ClienteListItemResponse.cs`
- `PulperiaPOS/Configuration/FeatureFlags.cs`
- `PulperiaPOS/ClientePage.xaml.cs`
- `PulperiaPOS/appsettings.json`
- `PulperiaPOS/appsettings.Development.json.example`

## Feature flag

Configuracion versionada:

```json
{
  "FeatureFlags": {
    "UseApiLogin": false,
    "UseClientesApi": false
  }
}
```

`UseClientesApi=false` conserva el flujo SQL actual.

`UseClientesApi=true` hace que `ClientePage` use `ClientesApiClient` para listado y busqueda. Si la API falla, no hay fallback automatico a SQL.

## Comportamiento WPF

Con `UseClientesApi=false`:

- `ClientePage` usa `SELECT * FROM cliente`;
- busqueda usa `SELECT * FROM cliente WHERE LOWER(nombre) LIKE @nombre`;
- comportamiento productivo actual queda conservado.

Con `UseClientesApi=true`:

- `ClientePage` consulta `GET /api/clientes`;
- busqueda usa `busqueda=texto`;
- errores API muestran mensaje seguro;
- 401 usa infraestructura central de sesion expirada;
- no se ejecuta SQL para listado/busqueda.

No se modifico `VentasPage`.

## Pruebas realizadas

| Prueba | Resultado |
|---|---|
| `/api/clientes` sin token | HTTP 401 |
| Token valido sin `Clientes.Ver` | HTTP 403 |
| Token valido con `Clientes.Ver` | HTTP 200 |
| Busqueda vacia | HTTP 200 |
| Busqueda con texto | HTTP 200 |
| Busqueda con caracteres especiales | HTTP 200 |
| Busqueda sin resultados | HTTP 200, 0 resultados |
| `limit` invalido | HTTP 400 |
| SQL total clientes | 162 |
| API total clientes | 162 |
| `UseClientesApi` versionado/local | `false` |
| `Authentication:EnableLegacyHashUpgrade` | `false` |

Las pruebas de token usaron JWT local efimero en memoria. No se imprimio ni guardo token ni signing key.

## Integridad

Validacion agregada por SQL:

- total clientes: 162;
- clientes con saldo: 79;
- comprobante vacio o nulo: 2.

No se ejecutaron `INSERT`, `UPDATE`, `DELETE`, scripts SQL ni migraciones.

## Limitaciones

- No existe columna `activo`; `soloActivos` no filtra estado por ahora.
- No se migro selector de cliente en ventas.
- No se migraron creacion, edicion, eliminacion ni liberacion de saldo.
- No se implemento paginacion visual en WPF.

## Recomendacion

Ejecutar Fase 3F: validar manualmente `UseClientesApi=true` en WPF con login API, revisar visualmente `ClientePage`, y luego migrar el selector de cliente de `VentasPage` solo en modo lectura.
