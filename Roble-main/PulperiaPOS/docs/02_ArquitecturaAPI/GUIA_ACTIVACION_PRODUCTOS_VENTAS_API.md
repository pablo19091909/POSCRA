# Guia de activacion - Productos de VentasPage por API

## Proposito

Activar temporalmente la lectura de productos desde `POS.Api` en `VentasPage`, manteniendo una ruta de regreso inmediata al SQL directo.

## Requisitos previos

- `POS.Api` debe estar ejecutandose por HTTPS.
- `/health` debe responder HTTP 200.
- `/health/database` debe responder HTTP 200.
- El usuario autenticado en WPF debe tener permiso `Inventario.Ver`.
- `Authentication:EnableLegacyHashUpgrade` debe permanecer en `false`.

## Activar

En el archivo local no versionado del WPF, establecer:

```json
{
  "FeatureFlags": {
    "UseVentasProductosApi": true
  }
}
```

No activar este flag en archivos versionados.

## Validar manualmente

1. Iniciar `POS.Api`.
2. Iniciar WPF.
3. Iniciar sesion con login API para tener JWT en memoria.
4. Abrir `VentasPage`.
5. Escribir texto en la busqueda de productos.
6. Confirmar que aparecen sugerencias.
7. Buscar por codigo o texto.
8. Agregar un producto al carrito.
9. Confirmar que nombre, precio, cantidad, subtotal y stock disponible se ven coherentes.
10. Probar un producto sin stock si existe y confirmar mensaje seguro.
11. No confirmar ni registrar ventas durante esta validacion.

## Validar error seguro

Con el flag activo, apagar temporalmente `POS.Api` y buscar un producto. El resultado esperado es un mensaje seguro sin fallback automatico a SQL.

## Desactivar

En el archivo local no versionado del WPF:

```json
{
  "FeatureFlags": {
    "UseVentasProductosApi": false
  }
}
```

Tambien puede eliminarse el valor local para volver al valor versionado por defecto.

## Seguridad

- No guardar tokens en disco.
- No copiar codigos, precios ni existencias reales en documentacion.
- No subir `appsettings.Development.json`.
- No publicar connection strings.
- No registrar ventas durante la prueba de lectura.

## Resultado esperado

- Flag apagado: `VentasPage` usa SQL directo.
- Flag encendido: `VentasPage` usa `GET /api/productos`.
- API apagada con flag encendido: error seguro, sin fallback SQL.
