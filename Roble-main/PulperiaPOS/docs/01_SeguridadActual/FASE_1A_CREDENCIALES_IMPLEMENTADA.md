# Fase 1A - Credenciales Implementada

Fecha: 2026-06-24

## 1. Alcance

Esta fase implementa solo proteccion temporal de credenciales para el POS WPF actual. No crea API, no cambia reglas de negocio y no modifica base de datos.

## 2. Estado anterior

Archivo afectado antes del cambio:

- `PulperiaPOS\DataAccess\DBConnection.cs`

Estado anterior:

- El archivo construia la cadena de conexion Azure SQL directamente en codigo.
- La cadena incluia servidor, base, usuario y password.
- `GetConnection()` usaba esa cadena estatica para abrir `SqlConnection`.
- Los errores se registraban con `ex.Message`.

## 3. Cambio realizado

Archivos nuevos:

- `PulperiaPOS\appsettings.json`
- `PulperiaPOS\appsettings.Development.json.example`
- `.gitignore`
- `logs\.gitkeep`

Archivos modificados:

- `PulperiaPOS\DataAccess\DBConnection.cs`
- `PulperiaPOS\PulperiaPOS.csproj`

Nuevo comportamiento:

1. `DBConnection.GetConnection()` mantiene la misma firma publica.
2. `DBConnection.GetConnectionString()` mantiene la misma firma publica.
3. La cadena se lee desde `ConnectionStrings:PosDatabase`.
4. El archivo base `appsettings.json` no contiene secreto real.
5. `appsettings.Development.json` es opcional y debe ser local.
6. Tambien se permite configurar la variable de entorno `POS_DATABASE_CONNECTION_STRING`.
7. Si no hay configuracion valida, la aplicacion falla con un mensaje controlado.
8. El log local no escribe la cadena de conexion ni la password.

## 4. Como configurar una maquina de desarrollo

Opcion recomendada con archivo local:

1. Copiar `PulperiaPOS\appsettings.Development.json.example`.
2. Renombrar la copia como `PulperiaPOS\appsettings.Development.json`.
3. Reemplazar los placeholders por los valores reales de desarrollo.
4. No compartir ni subir ese archivo.
5. Compilar y ejecutar la aplicacion.

Estructura esperada:

```json
{
  "ConnectionStrings": {
    "PosDatabase": "Server=SERVIDOR;Database=BASE;User Id=USUARIO;Password=CAMBIAR_ESTE_VALOR;Encrypt=True;TrustServerCertificate=False;"
  }
}
```

Opcion alternativa con variable de entorno:

```powershell
$env:POS_DATABASE_CONNECTION_STRING = "cadena-local-real"
```

La variable de entorno tiene prioridad sobre los archivos JSON.

## 5. Archivos que no deben subirse

Reglas agregadas en `.gitignore`:

- `appsettings.Development.json`
- `appsettings.Production.json`
- `.env`
- `*.bak`
- `*.log`
- `logs/*`
- `!logs/.gitkeep`

Archivos que si deben permanecer versionados:

- `PulperiaPOS\appsettings.json`
- `PulperiaPOS\appsettings.Development.json.example`

## 6. Limitacion de seguridad pendiente

Aunque la password ya no queda escrita en codigo fuente, el riesgo principal sigue existiendo mientras WPF se conecte directo a Azure SQL:

- El cliente WPF necesita una cadena de conexion valida.
- Un usuario con acceso a la maquina podria inspeccionar configuracion local.
- No hay autorizacion server-side por accion.
- La base sigue expuesta directamente al cliente hasta que exista API.

La correccion estructural sigue siendo migrar a arquitectura:

```text
WPF -> API -> Azure SQL
```

## 7. Como volver temporalmente al estado anterior

No se recomienda reintroducir secretos en codigo. Si la lectura de configuracion falla temporalmente:

1. Verificar que `PulperiaPOS\appsettings.Development.json` exista localmente.
2. Verificar que tenga `ConnectionStrings:PosDatabase`.
3. Verificar que el archivo se copie al output al compilar.
4. Alternativamente definir `POS_DATABASE_CONNECTION_STRING`.

Solo como ultima opcion operativa y fuera del repositorio, se puede ejecutar con la variable de entorno local para evitar modificar codigo.

## 8. Validacion esperada

Validaciones de Fase 1A:

- `DBConnection.cs` no contiene password real.
- `appsettings.json` no contiene password real.
- `appsettings.Development.json.example` solo contiene placeholders.
- `appsettings.Development.json` queda ignorado por Git.
- La solucion compila.
- Si falta configuracion local, el error no revela password.

## 9. Lo que no se modifico

No se modificaron:

- Queries de ventas.
- Queries de inventario.
- Queries de clientes.
- Queries de ingreso de caja.
- Queries de retiro de caja.
- Queries de cierre de caja.
- Scripts SQL.
- Tablas.
- Datos.
- Reglas financieras.
