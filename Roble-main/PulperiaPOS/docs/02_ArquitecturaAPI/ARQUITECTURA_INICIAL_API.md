# Arquitectura inicial de POS.Api

## Objetivo

`POS.Api` es la base para migrar gradualmente el POS WPF hacia una arquitectura WPF + API + Azure SQL, sin cambiar todavia los flujos funcionales existentes.

## Estructura

| Carpeta | Proposito |
| --- | --- |
| `Controllers` | Endpoints HTTP permitidos en la fase actual. |
| `Contracts` | DTOs de entrada/salida expuestos por la API. |
| `Application` | Lugar reservado para casos de uso futuros. |
| `Domain` | Lugar reservado para reglas de dominio futuras. |
| `Infrastructure/Data` | Acceso tecnico a SQL Server mediante `Microsoft.Data.SqlClient`. |
| `Infrastructure/Logging` | Manejo global de errores y mensajes seguros. |
| `Infrastructure/Security` | Constantes y configuracion inicial de seguridad/CORS. |
| `Configuration` | Nombres de claves de configuracion y variables de entorno. |
| `Health` | Validaciones de salud de servicio y base de datos. |
| `docs` | Documentacion local del proyecto API. |

## Endpoints iniciales

- `GET /health`: valida que la API responde.
- `GET /health/database`: valida apertura de conexion a SQL sin exponer secretos.
- `GET /api/system/version`: publica metadatos basicos de servicio.

## Excepciones

El middleware global devuelve siempre:

```json
{
  "traceId": "valor-generado",
  "message": "mensaje-seguro"
}
```

Los logs registran tipo de excepcion y `traceId`, sin cadenas de conexion ni secretos.

## Limites intencionales

- No hay endpoints de ventas.
- No hay endpoints de inventario.
- No hay endpoints de clientes.
- No hay endpoints de usuarios.
- No hay endpoints de caja.
- No hay JWT todavia.
- La WPF no consume esta API en esta fase.
