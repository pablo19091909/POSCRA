# Guia de activacion - Selector de cliente en VentasPage por API

## Proposito

Activar temporalmente el uso de `POS.Api` para cargar y buscar clientes desde el selector de `VentasPage`, manteniendo una ruta de regreso inmediata al SQL directo.

## Requisitos previos

- `POS.Api` debe estar ejecutandose por HTTPS.
- `/health` debe responder HTTP 200.
- `/health/database` debe responder HTTP 200.
- El usuario autenticado en WPF debe tener permiso `Clientes.Ver`.
- `Authentication:EnableLegacyHashUpgrade` debe permanecer en `false`.

## Activar

En el archivo local no versionado del WPF, establecer:

```json
{
  "FeatureFlags": {
    "UseVentasClienteSelectorApi": true
  }
}
```

No modificar archivos versionados para activaciones locales.

## Validar

1. Iniciar `POS.Api`.
2. Iniciar WPF.
3. Iniciar sesion con login API activo si se requiere token de sesion.
4. Abrir `VentasPage`.
5. Confirmar que el selector carga clientes.
6. Buscar por texto.
7. Seleccionar un cliente.
8. Confirmar que el saldo visual se mantiene coherente.
9. No finalizar ventas durante esta validacion.

## Desactivar

En el archivo local no versionado del WPF, establecer:

```json
{
  "FeatureFlags": {
    "UseVentasClienteSelectorApi": false
  }
}
```

Tambien puede eliminarse el valor local para volver al valor versionado por defecto.

## Verificacion de seguridad

- No guardar tokens en disco.
- No copiar credenciales a documentacion.
- No subir `appsettings.Development.json`.
- No publicar cadenas de conexion.
- No probar con datos personales en capturas o reportes.

## Resultado esperado

- Con el flag apagado, el selector usa SQL directo.
- Con el flag encendido, el selector usa `GET /api/clientes`.
- Si la API falla con el flag encendido, no debe haber fallback silencioso a SQL.
