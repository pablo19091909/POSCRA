# Plan de Rotacion de Credenciales Azure SQL

## 1. Motivo

La credencial anterior debe considerarse expuesta porque estuvo escrita en codigo fuente dentro de `PulperiaPOS\DataAccess\DBConnection.cs`.

Aunque se retire del codigo, cualquier copia historica del repositorio, binarios, respaldos, capturas o paquetes publicados podria conservarla.

## 2. Objetivos de la rotacion

- Invalidar la password expuesta.
- Configurar una password nueva fuera del repositorio.
- Mantener continuidad operativa.
- Evitar escribir secretos en documentos, comentarios, logs o ejemplos.

## 3. Orden seguro recomendado

1. Identificar la cuenta SQL usada por el POS WPF.
2. Confirmar quien conoce o usa esa cuenta.
3. Crear una ventana corta de mantenimiento.
4. Preparar `appsettings.Development.json` local en cada maquina autorizada.
5. Cambiar la password en Azure SQL.
6. Actualizar solo la configuracion local o variable de entorno en las maquinas autorizadas.
7. Probar conexion desde la aplicacion.
8. Confirmar login y carga de inventario/clientes.
9. Revocar credenciales antiguas si se creo una cuenta nueva.
10. Registrar fecha de rotacion sin incluir el secreto.

## 4. Estrategia recomendada

Opcion A: cambiar password de la cuenta actual.

- Ventaja: menor cambio.
- Riesgo: todas las instalaciones deben actualizarse al mismo tiempo.

Opcion B: crear una cuenta SQL nueva de transicion.

- Ventaja: permite probar antes de retirar la anterior.
- Riesgo: requiere administrar permisos correctamente.

Recomendacion:

- Si el POS esta en uso activo, crear cuenta nueva con permisos minimos equivalentes, probar, cambiar instalaciones y luego deshabilitar la cuenta anterior.

## 5. Configuracion local despues de rotar

En cada equipo:

1. Abrir o crear `PulperiaPOS\appsettings.Development.json`.
2. Actualizar `ConnectionStrings:PosDatabase` con la nueva credencial.
3. No guardar el archivo en commits.
4. Compilar o ejecutar para que el archivo se copie al output.

Alternativa:

```powershell
$env:POS_DATABASE_CONNECTION_STRING = "cadena-local-real"
```

No colocar esta variable en scripts versionados.

## 6. Verificacion

Verificaciones minimas:

- La aplicacion abre sin error de configuracion.
- Login funciona con un usuario autorizado.
- Inventario o clientes cargan correctamente.
- No aparece `Password=` real en `DBConnection.cs`.
- No aparece `appsettings.Development.json` como archivo listo para commit.
- Los logs no contienen cadena de conexion.

## 7. Plan de reversion

Si la nueva credencial falla:

1. No restaurar secretos en codigo.
2. Revertir localmente la configuracion a la credencial anterior solo si aun no fue revocada.
3. Si la anterior ya fue revocada, corregir permisos/password de la nueva cuenta en Azure SQL.
4. Validar conectividad con una maquina controlada.
5. Reintentar login y carga de datos.

## 8. Permisos minimos futuros

Mientras no exista API, la cuenta usada por WPF todavia necesita permisos amplios para los flujos actuales. En Fase futura, al migrar a API:

- WPF no debe conocer credenciales SQL.
- La API debe usar secreto por ambiente.
- La cuenta SQL de API debe tener permisos minimos.
- Operaciones destructivas deben ser reemplazadas por anulaciones auditadas.

## 9. Prohibiciones

No incluir:

- Password real.
- Cadena de conexion real.
- Capturas con secretos.
- Logs con secretos.
- Comentarios con secretos.
- Archivos `.bak` o `.env` con secretos en repositorio.
