# Fase 0 - Inventario Tecnico

Fecha de inventario: 2026-06-24

## 1. Alcance real analizado

Solucion analizada:

- `D:\POSCRA\Roble-main\PulperiaPOS\PulperiaPOS.sln`

Proyecto incluido en la solucion:

| Proyecto | Ruta | Tipo | Framework | Arranque |
|---|---|---|---|---|
| PulperiaPOS | `D:\POSCRA\Roble-main\PulperiaPOS\PulperiaPOS\PulperiaPOS.csproj` | WPF | `net8.0-windows` | Aplicacion WPF |

Evidencia:

- `PulperiaPOS.sln` referencia solo `PulperiaPOS\PulperiaPOS.csproj`.
- `dotnet sln .\PulperiaPOS.sln list` devuelve solo `PulperiaPOS\PulperiaPOS.csproj`.
- `PulperiaPOS\PulperiaPOS.csproj` define `OutputType=WinExe`, `TargetFramework=net8.0-windows` y `UseWPF=true`.

## 2. Proyectos o API encontrados

En el arbol actual de `D:\POSCRA\Roble-main\PulperiaPOS` no se encontro proyecto API agregado a la solucion ni proyecto `.csproj` adicional.

Busquedas realizadas sobre archivos fuente y proyecto:

- `POS.Api`
- `Swagger`
- `Swashbuckle`
- `Jwt`
- `Bearer`
- `AuthApiClient`
- `VentasApiClient`
- `ApiClientBase`
- `HttpClient`
- `Controller`
- `ApiController`
- `MapPost`
- `MapGet`
- `Authorize`
- `Authentication`
- `Authorization`

Resultado: no hay evidencia de API, cliente API, JWT, Swagger ni controladores dentro del alcance cargado.

Nota: si existe una API, no esta dentro de esta solucion/carpeta analizada. Debe adjuntarse la carpeta, rama o solucion correspondiente antes de iniciar una migracion real hacia API.

## 3. Carpetas relevantes encontradas

| Carpeta | Observacion |
|---|---|
| `.vs` | Metadatos de Visual Studio. No se considera codigo funcional. |
| `database` | Existe estructura `backups-notes`, `diagnostics`, `migrations`, `rollback`; no se encontraron archivos fuente o SQL dentro de esas carpetas en esta revision. |
| `docs` | Carpeta de documentacion existente. En esta fase se agregan documentos nuevos. |
| `logs` | Carpeta existente sin archivos encontrados en esta revision. |
| `PulperiaPOS` | Proyecto WPF principal y codigo funcional. |
| `scripts` | Existe `scripts\development`; no se encontraron scripts en esta revision. |

## 4. Dependencias NuGet importantes

Archivo: `PulperiaPOS\PulperiaPOS.csproj`

| Paquete | Version | Uso probable |
|---|---:|---|
| `System.Data.SqlClient` | 4.9.0 | Acceso directo a Azure SQL desde WPF. |
| `System.Data.SQLite` | 1.0.119 | Dependencia presente; no se encontro uso activo en codigo fuente. |
| `PdfSharpCore` | 1.3.67 | Reportes PDF de inventario. |
| `System.Drawing.Common` | 9.0.5 | Imagenes/impresion/PDF. |
| `System.IO.Ports` | 9.0.5 | Integracion con hardware/puertos. |
| `Microsoft.Extensions.Configuration.Json` | 9.0.4 | Configuracion JSON disponible, sin uso central encontrado para secretos. |

## 5. Fuentes de datos y configuracion

| Fuente | Evidencia | Estado |
|---|---|---|
| Azure SQL | `PulperiaPOS\DataAccess\DBConnection.cs`, campo `connectionString` | Activo. |
| SQLite | `PulperiaPOS\Data\pulperia.db`, paquete `System.Data.SQLite` | Archivo/dependencia existente; no se encontro uso activo. |
| App.config | `PulperiaPOS\App.config` contiene solo `VersionLocal` | No contiene connection string. |
| Script SQL externo | `C:\Users\pablo\OneDrive\Desktop\poscra database.sql` | Revisado como referencia de estructura actual. |

## 6. Credenciales y secretos encontrados

Hallazgo critico:

- Archivo: `PulperiaPOS\DataAccess\DBConnection.cs`
- Lineas relevantes: 10-14
- Evidencia: connection string de Azure SQL embebido en codigo, incluyendo servidor, usuario y password.
- Riesgo: cualquier copia del ejecutable o codigo fuente expone acceso directo a base de datos.

No se encontraron tokens JWT, refresh tokens, secretos de API ni `appsettings.json` activos dentro del proyecto.

## 7. Codigo activo, legado o no utilizado

### Codigo activo

| Area | Archivos |
|---|---|
| Login | `LoginWindow.xaml`, `LoginWindow.xaml.cs`, `Seguridad.cs`, `UserSession.cs` |
| Ventanas por rol | `VentanaAdministrador.xaml.cs`, `VentanaAnfitrion.xaml.cs` |
| Ventas | `VentasPage.xaml.cs`, `Views\VentasCrudWindow.xaml.cs`, `Views\DetalleVentaWindow.xaml.cs` |
| Caja | `IngresoCajaPage.xaml.cs`, `Views\RetirosCajaPage.xaml.cs`, `Views\CierreCajaPage.xaml.cs`, `CajaHelper.cs` |
| Inventario/productos | `InventarioWindow.xaml.cs`, `ProductoForm.xaml.cs` |
| Clientes/saldos | `ClientePage.xaml.cs`, `ClienteForm.xaml.cs`, `SaldoLiberadoPage.xaml.cs` |
| Usuarios | `VentanaUsuarios.xaml.cs`, `VentanaEditarUsuario.xaml.cs` |
| Donaciones | `DonacionesPage.xaml.cs` |
| Tipo cambio | `TipoCambioWindow.xaml.cs`, `TipoCambioHelper.cs` |
| Impresion/caja fisica | `RawPrinterHelper.cs` |

### Codigo o artefactos potencialmente legados/no usados

| Elemento | Evidencia | Observacion |
|---|---|---|
| SQLite | `PulperiaPOS\Data\pulperia.db`, paquete `System.Data.SQLite` | No se encontro uso de `SQLiteConnection` en codigo fuente. |
| Creacion comentada de `saldo_liberado` | `ClientePage.xaml.cs`, bloque comentado cerca de `BtnLiberarSaldo_Click` | Indica migracion manual previa o deuda tecnica. |
| `AzureConnectionTester` | `PulperiaPOS\DataAccess\AzureConnectionTester.cs` | Clase utilitaria para probar conexion; no se encontro punto de llamada evidente. |
| `MainWindow` | `MainWindow.xaml.cs` | Existe, pero el flujo visible por login usa `VentanaAdministrador`/`VentanaAnfitrion`. |

## 8. Resumen de riesgo tecnico por arquitectura actual

La arquitectura actual es:

```text
WPF -> DBConnection -> Azure SQL
```

No existe una capa API en el alcance analizado. Las ventanas WPF ejecutan SQL directo, contienen reglas de negocio y muestran errores tecnicos al usuario. Para Fase 1, la prioridad debe ser introducir una API con servicios transaccionales sin romper los flujos existentes.

## 9. Archivos funcionales revisados

- `PulperiaPOS.sln`
- `PulperiaPOS\PulperiaPOS.csproj`
- `PulperiaPOS\App.config`
- `PulperiaPOS\App.xaml`
- `PulperiaPOS\App.xaml.cs`
- `PulperiaPOS\LoginWindow.xaml.cs`
- `PulperiaPOS\Seguridad.cs`
- `PulperiaPOS\UserSession.cs`
- `PulperiaPOS\VentanaAdministrador.xaml.cs`
- `PulperiaPOS\VentanaAnfitrion.xaml.cs`
- `PulperiaPOS\VentanaUsuarios.xaml.cs`
- `PulperiaPOS\VentanaEditarUsuario.xaml.cs`
- `PulperiaPOS\VentasPage.xaml.cs`
- `PulperiaPOS\Views\VentasCrudWindow.xaml.cs`
- `PulperiaPOS\Views\DetalleVentaWindow.xaml.cs`
- `PulperiaPOS\IngresoCajaPage.xaml.cs`
- `PulperiaPOS\Views\RetirosCajaPage.xaml.cs`
- `PulperiaPOS\Views\CierreCajaPage.xaml.cs`
- `PulperiaPOS\CajaHelper.cs`
- `PulperiaPOS\InventarioWindow.xaml.cs`
- `PulperiaPOS\ProductoForm.xaml.cs`
- `PulperiaPOS\ClientePage.xaml.cs`
- `PulperiaPOS\ClienteForm.xaml.cs`
- `PulperiaPOS\Cliente.cs`
- `PulperiaPOS\SaldoLiberadoPage.xaml.cs`
- `PulperiaPOS\DonacionesPage.xaml.cs`
- `PulperiaPOS\TipoCambioWindow.xaml.cs`
- `PulperiaPOS\TipoCambioHelper.cs`
- `PulperiaPOS\RawPrinterHelper.cs`
- `PulperiaPOS\DataAccess\DBConnection.cs`
- `PulperiaPOS\DataAccess\AzureConnectionTester.cs`
- `C:\Users\pablo\OneDrive\Desktop\poscra database.sql`

## 10. Confirmacion de Fase 0

Esta fase solo documenta el estado actual. No se modifico codigo funcional, configuracion, scripts SQL existentes, connection strings ni base de datos.
