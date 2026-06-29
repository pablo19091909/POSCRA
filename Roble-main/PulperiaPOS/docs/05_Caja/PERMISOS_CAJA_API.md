# Permisos Caja API

## Permisos

- `Caja.Ver`
- `Caja.Abrir`
- `Caja.Ingresar`
- `Caja.Retirar`
- `Caja.PreCerrar`
- `Caja.Cerrar`

Se conserva `Caja.VerResumen` para compatibilidad con permisos existentes.

## Asignacion propuesta actual

| Rol | Permisos |
| --- | --- |
| Administrador | Ver, abrir, ingresar, retirar, pre-cerrar, cerrar, reabrir, ver resumen |
| Anfitrion | Ver, pre-cerrar, cerrar, ver resumen |

La API exige JWT y permiso minimo por endpoint. La seguridad WPF es solo UX; POS.Api es la autoridad.

## Mapa de endpoints

| Endpoint | Permiso |
| --- | --- |
| `GET /api/caja/turnos/abierto` | `Caja.Ver` |
| `POST /api/caja/turnos/abrir` | `Caja.Abrir` |
| `POST /api/caja/ingresos` | `Caja.Ingresar` |
| `POST /api/caja/retiros` | `Caja.Retirar` |
| `GET /api/caja/turnos/{id}/pre-cierre` | `Caja.PreCerrar` |
| `POST /api/caja/turnos/{id}/cerrar` | `Caja.Cerrar` |
| `GET /api/caja/turnos/{id}/movimientos` | `Caja.Ver` |
