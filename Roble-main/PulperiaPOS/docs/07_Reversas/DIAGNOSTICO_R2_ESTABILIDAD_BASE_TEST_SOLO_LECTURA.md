# Diagnostico R2 - Estabilidad Base Test Solo Lectura

Fecha UTC: 2026-07-02T01:15:11.8552841+00:00

## Alcance

Diagnostico R2 ejecutado exclusivamente con operaciones de lectura:

- GET `/health`.
- GET `/health/database`.
- GET `/api/system/version`.
- Apertura de conexion SQL.
- SELECT `1`.
- SELECT agregado de marca `Environment=Test`.
- SELECT agregado ampliado de caja, idempotencia y reversas.

No se ejecutaron POST, PUT, PATCH, DELETE, INSERT, UPDATE, MERGE, ALTER, CREATE, DROP, TRUNCATE, migraciones, operaciones WPF ni operaciones de negocio.

## Auditoria de `/health/database`

`/health/database` esta implementado en `POS.Api.Controllers.HealthController` y delega en `DatabaseHealthCheck`.

Hallazgos:

- Usa `IDatabaseConnectionFactory`.
- La implementacion registrada es `SqlConnectionFactory`.
- `SqlConnectionFactory` usa `Microsoft.Data.SqlClient`.
- La prueba abre una conexion real.
- Ejecuta unicamente `SELECT 1`.
- Usa `CommandTimeout = 5`.
- Devuelve HTTP 200 si la apertura y consulta minima pasan.
- Devuelve HTTP 503 con respuesta segura si falla.
- Registra tipo tecnico de excepcion y traceId, sin cadena de conexion.
- No hay cache observable en el health check.

`/health` no valida base de datos; solo confirma que el servicio API responde. `/health/database` confirma una lectura minima puntual, pero no sustituye una lectura real de tablas de negocio o infraestructura.

## Retry, timeout y pooling

- Retry explicito: no identificado en codigo de API para SQL.
- `EnableRetryOnFailure`: no identificado.
- Timeout de `/health/database`: `CommandTimeout = 5`.
- Timeout de lecturas reales de repositorios: no se identifico un timeout centralizado equivalente en la fabrica de conexion.
- Pooling: no configurado explicitamente en codigo; queda sujeto al comportamiento por defecto de `Microsoft.Data.SqlClient` y a la cadena local, sin exponer valores.

## Health checks

Prueba usando `https://localhost:7046`:

| Endpoint | Resultado |
| --- | --- |
| `/health` | HTTP 200 |
| `/health/database` | HTTP 200 |
| `/api/system/version` | HTTP 200 |

Observacion TLS: `https://127.0.0.1:7046` presento fallo de negociacion TLS en el cliente de diagnostico. Con `https://localhost:7046` los GET respondieron correctamente. Esto apunta a diferencia de nombre/certificado local, no a fallo SQL.

## Ciclos de lectura SQL

Niveles ejecutados:

- Nivel 1: apertura de conexion y `SELECT 1`.
- Nivel 2: validacion agregada de `Environment=Test`.
- Nivel 3: lectura agregada ampliada de caja, idempotencia y reversas.

Resultado final:

| Ciclo | Apertura | Nivel 1 | Nivel 2 | Nivel 3 |
| --- | --- | --- | --- | --- |
| 1 | OK | OK | OK | OK |
| 2 | OK | OK | OK | OK |
| 3 | OK | OK | OK | OK |
| 4 | OK | OK | OK | OK |
| 5 | OK | OK | OK | OK |

Tiempos observados:

- Apertura: entre 897 ms y 1179 ms.
- Nivel 1: entre 186 ms y 193 ms.
- Nivel 2: entre 125 ms y 131 ms.
- Nivel 3: entre 123 ms y 135 ms.

Se ejecutaron dos corridas preliminares no clasificables por error del probe temporal:

- Primera corrida: Nivel 2 y Nivel 3 fallaron con `SqlException 207` por nombre de columna incorrecto en el probe.
- Segunda corrida: Nivel 3 fallo con `SqlException 207` por nombre de columna incorrecto en el probe.

Esas corridas no indican inestabilidad de la base. La corrida final uso el esquema real y paso 5/5.

## Diferencia entre health y lectura real

`/health/database` valida apertura y consulta minima contra la base. La lectura real agregada valida tambien que tablas de soporte de caja, idempotencia y reversas respondan. En esta ejecucion, ambos caminos fueron consistentes: health 200 y lectura real 5/5 OK.

## Error SQL sanitizado y punto de fallo

No hubo error SQL en la corrida final valida.

Errores preliminares sanitizados:

- Tipo: `SqlException`.
- Codigo: `207`.
- Clase: `16`.
- Estado: `1`.
- Punto de fallo: consulta de diagnostico temporal, no API productiva ni base inestable.

No se documentaron mensajes internos, servidor, base, usuario, cadena de conexion, token ni identificadores de negocio.

## Clasificacion tecnica

Clasificacion final: **A - Estable para reintentar Compuerta A**.

Justificacion:

- API disponible por `localhost`.
- `/health/database` respondio 200.
- Apertura SQL paso 5/5.
- `SELECT 1` paso 5/5.
- Marca `Environment=Test` paso 5/5.
- Lectura agregada ampliada paso 5/5.
- No se reprodujo `SqlException 40613` ni `SqlException 5` durante la corrida final valida.

## Flags finales

WPF local:

- `UseCajaApiRead=true`.
- `UseCajaApiOpenWrite=false`.
- `UseCajaApiIngresoWrite=false`.
- `UseCajaApiRetiroWrite=false`.
- `UseCajaApiCierreWrite=false`.
- `UseVentasApiWrite=false`.
- `UseVentasApiEfectivoWrite=false`.
- `UseVentasApiReversaWrite=false`.

API local:

- `EnableVentasApiWrite=false`.
- `EnableCajaApiWrite=false`.
- `EnableVentasApiEfectivoCajaWrite=false`.
- `EnableVentasApiReversaCajaWrite=false`.
- `EnableLegacyHashUpgrade=false`.

## Cero escrituras

Confirmado para este diagnostico:

- No se uso WPF.
- No se ejecutaron endpoints de escritura.
- No se ejecutaron scripts SQL.
- No se ejecutaron migraciones.
- No se modifico configuracion.
- No se modifico esquema.
- No se modificaron datos.

## Accion correctiva recomendada

No aplicar cambios todavia.

Recomendacion tecnica:

1. Para pruebas locales de API, usar `https://localhost:7046` en vez de `https://127.0.0.1:7046` mientras el certificado de desarrollo este asociado a localhost.
2. Mantener los flags de escritura apagados hasta iniciar formalmente el reintento de Compuerta A.
3. Si el error `40613` reaparece, tratarlo como indisponibilidad transitoria de Azure SQL y repetir health + lectura agregada antes de cualquier prueba destructiva.
4. Evaluar en una fase posterior una politica explicita y limitada de retry para errores transitorios SQL, sin aplicarla en este R2.

## Criterio para reintentar Compuerta A

Reintentar Compuerta A solo si antes de iniciar:

- `/health` responde 200.
- `/health/database` responde 200.
- `/api/system/version` responde 200.
- Lectura agregada R2 pasa al menos 5/5.
- Flags de ventas/caja/reversa siguen apagadas antes de habilitar la compuerta autorizada.
- No hay turno abierto inesperado ni idempotencias en proceso segun lectura agregada.

