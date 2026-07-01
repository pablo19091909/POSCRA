# Feature flags VentaEfectivo Caja

## WPF

| Flag | Estado por defecto |
| --- | --- |
| `UseVentasApiWrite` | `false` |
| `UseVentasApiEfectivoWrite` | `false` |
| `UseCajaApiRead` | `true` en entorno local de migracion |
| `UseCajaApiOpenWrite` | `false` |
| `UseCajaApiIngresoWrite` | `false` |
| `UseCajaApiRetiroWrite` | `false` |
| `UseCajaApiCierreWrite` | `false` |

## POS.Api

| Flag | Estado por defecto |
| --- | --- |
| `EnableVentasApiWrite` | `false` |
| `EnableCajaApiWrite` | `false` |
| `EnableVentasApiEfectivoCajaWrite` | `false` |
| `EnableLegacyHashUpgrade` | `false` |

## Regla de activacion futura

La venta efectiva con Caja API solo puede ejecutarse si:

```text
UseVentasApiEfectivoWrite=true
EnableVentasApiWrite=true
EnableCajaApiWrite=true
EnableVentasApiEfectivoCajaWrite=true
Environment=Test
turno de caja Abierto
```

## Estado de esta fase

Todos los flags de escritura terminaron apagados. La implementacion queda bloqueada.
