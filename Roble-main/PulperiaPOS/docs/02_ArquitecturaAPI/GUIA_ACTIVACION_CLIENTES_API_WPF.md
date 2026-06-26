# Guia de activacion Clientes API en WPF

## Reglas

- No compartir usuarios, contrasenas ni JWT.
- No guardar tokens en archivos.
- No activar `EnableLegacyHashUpgrade`.
- No modificar clientes, saldos ni ventas durante la prueba.
- Revertir `UseClientesApi=false` al terminar.

## Requisitos

1. POS.Api levantada por HTTPS.
2. `/health`, `/health/database` y `/api/system/version` en HTTP 200.
3. Login WPF por API disponible si se va a probar listado con JWT real.

## Activar temporalmente

Editar solo localmente:

```text
PulperiaPOS/appsettings.Development.json
```

Usar:

```json
{
  "FeatureFlags": {
    "UseApiLogin": true,
    "UseClientesApi": true
  }
}
```

No copiar ni imprimir connection strings si el archivo local las contiene.

## Probar

1. Iniciar POS.Api.
2. Iniciar WPF.
3. Iniciar sesion por API con usuario autorizado.
4. Abrir `ClientePage`.
5. Confirmar que la lista carga.
6. Probar busqueda por nombre.
7. No agregar, editar, eliminar ni liberar saldo.
8. Cerrar sesion.

Si aparece error de API:

- revisar que el login fue por API;
- revisar que el usuario tenga `Clientes.Ver`;
- no esperar fallback automatico a SQL.

## Revertir

Cambiar localmente:

```json
{
  "FeatureFlags": {
    "UseClientesApi": false
  }
}
```

Con `UseClientesApi=false`, `ClientePage` vuelve al listado SQL directo.

## Verificar que no se sube configuracion local

`.gitignore` debe cubrir:

```text
appsettings.Development.json
**/appsettings.Development.json
```

## Alcance no migrado

Siguen por SQL directo:

- agregar cliente;
- editar cliente;
- eliminar cliente;
- liberar saldo;
- reporte de saldos;
- historial de liberaciones;
- selector de cliente en ventas.
