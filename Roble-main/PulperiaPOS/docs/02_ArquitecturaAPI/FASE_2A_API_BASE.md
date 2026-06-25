# Fase 2A - API base POS

## Alcance implementado

- Se creo el proyecto ASP.NET Core Web API `POS.Api` en .NET 8.
- Se agrego `POS.Api` a `PulperiaPOS.sln`.
- Se mantuvo la aplicacion WPF sin conexion a la API.
- Se agregaron unicamente los endpoints permitidos:
  - `GET /health`
  - `GET /health/database`
  - `GET /api/system/version`
- Se retiro el endpoint de plantilla `WeatherForecast`.
- Se configuro Swagger/OpenAPI solo para entorno Development.
- Se configuro CORS con politica restrictiva `WpfLocalClient`, sin `AllowAnyOrigin`.
- Se agrego manejo global de excepciones con respuesta estandar `{ traceId, message }`.

## Resultado de compilacion

Comando ejecutado:

```powershell
dotnet build .\PulperiaPOS.sln /p:UseAppHost=false /p:OutDir=obj\CodexBuild2A\
```

Resultado:

- Compilacion correcta.
- `POS.Api` compilo correctamente.
- `PulperiaPOS` compilo correctamente.
- 0 errores.
- 0 advertencias en la salida dedicada de validacion.

## Resultado de ejecucion local

La API se ejecuto en Development en `http://127.0.0.1:5089`.

Resultados:

| Endpoint | Resultado |
| --- | --- |
| `GET /health` | 200, estado seguro `healthy` |
| `GET /api/system/version` | 200, servicio `POS.Api` |
| `GET /swagger/index.html` | 200, Swagger disponible en Development |
| `GET /health/database` | 503, error seguro sin secreto |

`/health/database` uso una variable temporal `POS_API_DATABASE_CONNECTION_STRING` sin imprimir su valor. La apertura de conexion fallo con error de SQL y se devolvio 503 sin exponer servidor, base, usuario, password ni cadena de conexion.

## Confirmaciones de seguridad

- No se creo `POS.Api/appsettings.Development.json`.
- Se creo solo `POS.Api/appsettings.Development.json.example` con placeholders.
- `POS.Api/appsettings.json` no contiene secretos reales.
- `.gitignore` incluye reglas explicitas para `POS.Api/appsettings.Development.json` y `POS.Api/appsettings.Production.json`.
- No se copio la configuracion local WPF hacia el proyecto API.
- No se agregaron logs con cadenas de conexion.
- No se agrego JWT todavia; solo se dejo estructura inicial de seguridad.

## Validacion WPF

- La solucion completa compilo incluyendo `PulperiaPOS`.
- No se conecto la WPF a la API.
- No se modifico comportamiento funcional de WPF.

## Base de datos

- No se modificaron scripts SQL.
- No se modificaron tablas.
- No se insertaron, actualizaron ni borraron datos.
- La unica prueba de base de datos fue apertura de conexion desde `/health/database`.

## Hallazgos de busqueda de secretos

Escaneo realizado excluyendo `bin/`, `obj/`, `appsettings.Development.json` y `appsettings.Production.json`. Se reportan solo archivo y tipo de hallazgo, sin valores.

| Archivo | Tipo |
| --- | --- |
| `docs/00_InventarioTecnico/FASE_0_INVENTARIO_TECNICO.md` | Azure SQL, ConnectionString, Password, Token/Key |
| `docs/00_InventarioTecnico/FLUJOS_CRITICOS_ACTUALES.md` | Password |
| `docs/00_InventarioTecnico/MATRIZ_MODULOS_Y_SQL_DIRECTO.md` | Azure SQL, ConnectionString |
| `docs/01_SeguridadActual/FASE_1A_CREDENCIALES_IMPLEMENTADA.md` | Azure SQL, ConnectionString, Database, Password, Server, User Id |
| `docs/01_SeguridadActual/FASE_1B_VALIDACION_CONFIGURACION.md` | Azure SQL, ConnectionString, Password |
| `docs/01_SeguridadActual/HALLAZGOS_INICIALES_SEGURIDAD.md` | Azure SQL, ConnectionString, Password, Token/Key |
| `docs/01_SeguridadActual/PLAN_ROTACION_CREDENCIALES_AZURE_SQL.md` | Azure SQL, ConnectionString, Password |
| `docs/03_BaseDatos/TABLAS_Y_RELACIONES_ENCONTRADAS.md` | Password |
| `POS.Api/appsettings.Development.json.example` | ConnectionString, Database, Password, Server, User Id |
| `POS.Api/appsettings.json` | ConnectionString |
| `POS.Api/Configuration/ConfigurationKeys.cs` | ConnectionString |
| `POS.Api/Controllers/HealthController.cs` | Token/Key |
| `POS.Api/Health/DatabaseHealthCheck.cs` | Token/Key |
| `POS.Api/Health/IDatabaseHealthCheck.cs` | Token/Key |
| `POS.Api/Infrastructure/Data/SqlConnectionFactory.cs` | ConnectionString |
| `PulperiaPOS/App.config` | Token/Key |
| `PulperiaPOS/App.xaml` | Token/Key |
| `PulperiaPOS/App.xaml.cs` | Azure SQL |
| `PulperiaPOS/appsettings.Development.json.example` | ConnectionString, Database, Password, Server, User Id |
| `PulperiaPOS/appsettings.json` | ConnectionString |
| `PulperiaPOS/ClientePage.xaml` | Token/Key |
| `PulperiaPOS/Data/pulperia.db` | Password |
| `PulperiaPOS/DataAccess/AzureConnectionTester.cs` | Azure SQL |
| `PulperiaPOS/DataAccess/DBConnection.cs` | ConnectionString |
| `PulperiaPOS/LoginWindow.xaml` | Password |
| `PulperiaPOS/LoginWindow.xaml.cs` | Password |
| `PulperiaPOS/Seguridad.cs` | Password |
| `PulperiaPOS/VentanaEditarUsuario.xaml` | Password |
| `PulperiaPOS/VentanaEditarUsuario.xaml.cs` | Password |
| `PulperiaPOS/VentanaUsuarios.xaml` | Password |
| `PulperiaPOS/VentanaUsuarios.xaml.cs` | Password |
| `PulperiaPOS/VentasPage.xaml` | Token/Key |

Nota: varios hallazgos corresponden a placeholders, nombres de propiedades, controles de UI de password o claves XAML, no necesariamente a secretos reales. La revision de valores reales queda fuera de este documento para no exponer informacion sensible.

## Riesgos pendientes

- `/health/database` aun no confirma conexion exitosa desde la API en este entorno.
- La WPF sigue usando acceso directo a SQL.
- La autenticacion y autorizacion de API todavia no estan implementadas.
- No existe estrategia definitiva de configuracion por ambiente para despliegue.
- Los documentos historicos siguen conteniendo referencias sensibles o terminos de seguridad que deben revisarse sin publicar valores.

## Recomendacion siguiente

Antes de migrar pantallas, ejecutar una Fase 2B para estabilizar configuracion local/deployment de `POS.Api`, confirmar apertura exitosa de SQL con `Microsoft.Data.SqlClient`, definir autenticacion/autorizacion base y preparar contratos sin conectar WPF todavia.
