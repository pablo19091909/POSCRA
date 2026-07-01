# Validacion no fallback SQL lectura WPF Caja

Fecha UTC: 2026-06-30.

## Revision de codigo

Archivos revisados:

- `PulperiaPOS/CajaApiReadStatusViewHelper.cs`
- `PulperiaPOS/ApiClients/CajaApiClient.cs`
- `PulperiaPOS/IngresoCajaPage.xaml.cs`
- `PulperiaPOS/Views/RetirosCajaPage.xaml.cs`
- `PulperiaPOS/Views/CierreCajaPage.xaml.cs`

## Confirmacion

Para el indicador de lectura Caja API:

- la ruta usa `CajaApiClient`;
- no usa `DBConnection`;
- no usa `CajaHelper`;
- no consulta `ingreso_caja`, `retiro_caja` ni `cierre_caja`;
- no imprime comprobantes;
- no escribe movimientos ni idempotencias;
- si la API no responde, muestra mensaje seguro y no completa con historicos SQL.

## API no disponible

El operador valido WPF con POS.Api detenida. Resultado:

- mensaje seguro de servicio no disponible;
- sin stack trace;
- sin host, puerto, endpoint ni detalle tecnico;
- sin fallback SQL;
- sin afirmar turno cerrado o inexistente.

## Nota sobre pantallas historicas

Las pantallas pueden conservar grillas o totales historicos propios por compatibilidad del flujo anterior. Esa informacion no se usa para construir el indicador Caja API ni para confirmar estado del turno API.
