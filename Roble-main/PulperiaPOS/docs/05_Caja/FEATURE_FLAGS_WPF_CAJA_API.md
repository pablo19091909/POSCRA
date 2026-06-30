# Feature flags WPF Caja API

Fecha/hora UTC: 2026-06-30 02:13:13 UTC

## Flags agregados

Todos los flags quedan versionados en `false` por defecto:

- `UseCajaApiRead=false`;
- `UseCajaApiOpenWrite=false`;
- `UseCajaApiIngresoWrite=false`;
- `UseCajaApiRetiroWrite=false`;
- `UseCajaApiCierreWrite=false`.

Flags relacionados existentes:

- `UseVentasApiWrite=false`;
- `EnableVentasApiWrite=false`;
- `EnableCajaApiWrite=false`.

## Semantica

`UseCajaApiRead`:

- habilita lecturas WPF contra Caja API;
- no habilita escrituras;
- si esta apagado, `CajaApiClient` no llama a la API.

`UseCajaApiOpenWrite`:

- reservado para futura apertura por API;
- mientras este apagado, apertura historica no se sustituye.

`UseCajaApiIngresoWrite`:

- reservado para futuro ingreso por API;
- cuando se active, no debe existir fallback automatico a `ingreso_caja`.

`UseCajaApiRetiroWrite`:

- reservado para futuro retiro por API;
- cuando se active, no debe existir fallback automatico a `retiro_caja`.

`UseCajaApiCierreWrite`:

- reservado para futuro cierre por API;
- cuando se active, no debe existir fallback automatico a `cierre_caja`.

## Barrera final del servidor

Aunque WPF active un flag de escritura en una fase futura, POS.Api debe mantener la barrera final:

- `EnableCajaApiWrite=false` bloquea escrituras;
- `EnvironmentSafety` debe exigir `Environment=Test` durante pruebas;
- la API debe devolver error seguro si escritura no esta habilitada.

## Estado final de esta fase

Todos los flags quedaron apagados en archivos versionados.

