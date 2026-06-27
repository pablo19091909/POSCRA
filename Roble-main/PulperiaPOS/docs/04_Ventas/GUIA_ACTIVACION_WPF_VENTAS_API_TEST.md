# Guia de activacion WPF Ventas API Test

## Requisitos

- API ejecutandose por HTTPS local.
- Login API activo y usuario con permiso `Ventas.Crear`.
- Base marcada explicitamente como `Environment=Test`.
- Configuracion local no versionada disponible.

## Flags requeridos

WPF local:

```json
{
  "FeatureFlags": {
    "UseVentasApiWrite": true
  }
}
```

API local:

```json
{
  "FeatureFlags": {
    "EnableVentasApiWrite": true
  },
  "EnvironmentSafety": {
    "RequiredDatabaseEnvironment": "Test",
    "BlockWritesUnlessDatabaseEnvironmentMatches": true
  }
}
```

Los archivos versionados deben permanecer con ambos flags apagados.

## Validacion previa

1. Levantar POS.Api.
2. Confirmar `/health` = 200.
3. Confirmar `/health/database` = 200.
4. Confirmar `/api/system/version` = 200.
5. Iniciar sesion en WPF por API.
6. Abrir `VentasPage` y confirmar indicador discreto `API Test`.

## Prueba controlada

Usar solamente datos sinteticos de Test. No usar clientes, productos ni ventas reales.

1. Seleccionar cliente sintetico.
2. Agregar producto sintetico con stock suficiente.
3. Elegir un metodo soportado.
4. Confirmar venta.
5. Verificar que el recibo solo se imprime si la API confirma exito.
6. Verificar conteos agregados despues de la prueba.

## Reversion

1. Cambiar `UseVentasApiWrite=false` en configuracion local WPF.
2. Cambiar `EnableVentasApiWrite=false` en configuracion local API.
3. Reiniciar WPF y API.
4. Confirmar que el indicador `API Test` desaparece.
5. Confirmar que `VentasPage` vuelve a ruta SQL historica.
