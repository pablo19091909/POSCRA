# Guia de activacion temporal del login API en WPF

## Reglas

- No compartir usuarios, contrasenas ni JWT.
- No guardar tokens en archivos.
- No subir `appsettings.Development.json`.
- Mantener `Authentication:EnableLegacyHashUpgrade=false` en POS.Api.
- Revertir `UseApiLogin=false` al terminar la prueba.

## Iniciar POS.Api

Desde la raiz de la solucion:

```powershell
dotnet run --project .\POS.Api\POS.Api.csproj --launch-profile https
```

Verificar:

```powershell
Invoke-WebRequest -UseBasicParsing https://localhost:7046/health
Invoke-WebRequest -UseBasicParsing https://localhost:7046/health/database
Invoke-WebRequest -UseBasicParsing https://localhost:7046/api/system/version
```

Los tres endpoints deben responder HTTP 200.

## Configurar BaseUrl local

Crear o editar localmente:

```text
PulperiaPOS/appsettings.Development.json
```

Usar esta estructura, sin agregar secretos nuevos:

```json
{
  "Api": {
    "BaseUrl": "https://localhost:7046/"
  },
  "FeatureFlags": {
    "UseApiLogin": true
  }
}
```

Si el archivo tambien contiene `ConnectionStrings:PosDatabase`, no imprimirlo ni compartirlo.

## Activar login API

1. Confirmar que POS.Api esta levantada por HTTPS.
2. Cambiar solo localmente `FeatureFlags:UseApiLogin=true`.
3. Iniciar WPF.
4. Ingresar con una cuenta real autorizada.
5. Confirmar que abre la ventana correspondiente al rol.
6. Cerrar sesion.
7. Confirmar que vuelve a `LoginWindow`.

## Volver a login SQL

Cambiar localmente:

```json
{
  "FeatureFlags": {
    "UseApiLogin": false
  }
}
```

Reiniciar WPF y probar login normal.

## Prueba recomendada

- Probar un usuario que aun use SHA-256 legado.
- Probar el usuario ya migrado a BCrypt.
- Probar credenciales invalidas.
- Probar API apagada.
- Probar varios intentos fallidos hasta observar mensaje de limite.
- No copiar el JWT.
- No documentar usuario ni contrasena.

## Confirmar que el archivo local no se publica

Revisar `.gitignore`:

```text
appsettings.Development.json
POS.Api/appsettings.Development.json
**/appsettings.Development.json
```

El archivo local `PulperiaPOS/appsettings.Development.json` no debe aparecer en commits.

## Confirmar login SQL disponible

1. Dejar `UseApiLogin=false`.
2. Iniciar WPF.
3. Ingresar con cuenta real autorizada.
4. Confirmar que abre la ventana por rol.

Este modo sigue usando `DBConnection.GetConnection()` y la consulta SQL existente.
