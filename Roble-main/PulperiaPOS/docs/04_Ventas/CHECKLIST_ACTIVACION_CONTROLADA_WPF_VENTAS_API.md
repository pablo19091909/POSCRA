# Checklist de activacion controlada WPF Ventas API

Usar solo en ambiente Test o ambiente no productivo controlado. No usar en produccion hasta completar caja operacional, anulaciones/devoluciones y aprobacion formal.

## Antes de activar

- Confirmar que la base tiene marca explicita `Environment=Test`.
- Confirmar que POS.Api responde:
  - `/health` HTTP 200.
  - `/health/database` HTTP 200.
  - `/api/system/version` HTTP 200.
- Confirmar usuario con permiso `Ventas.Crear`.
- Confirmar datos sinteticos `API_TEST_` disponibles.
- Confirmar que no se usaran Dolares, Donacion ni pagos combinados.
- Confirmar que no se usaran clientes o productos historicos para pruebas API.
- Confirmar que no se imprimiran ni guardaran tokens, credenciales ni llaves de idempotencia.

## Activacion temporal

En configuracion local no versionada:

- WPF: activar `FeatureFlags:UseVentasApiWrite`.
- POS.Api: activar `FeatureFlags:EnableVentasApiWrite`.

No cambiar valores versionados por defecto.

## Pruebas permitidas

- Efectivo exacto.
- Efectivo con vuelto.
- Tarjeta.
- SINPE.
- Saldo Cliente.
- Stock insuficiente.
- Saldo insuficiente.
- Cliente General.
- Recibo posterior a exito API.
- Reintento controlado/idempotencia sin crear duplicados.

## Cierre obligatorio

- Restaurar WPF `FeatureFlags:UseVentasApiWrite=false`.
- Restaurar POS.Api `FeatureFlags:EnableVentasApiWrite=false`.
- Verificar que `.gitignore` cubre `appsettings.Development.json`.
- Verificar integridad agregada de ventas, detalles, pagos, auditorias e idempotencias.
- Confirmar cero cambios de caja, ingresos, retiros y cierres.
- Detener POS.Api.
- Confirmar puerto `7046` libre.

## Estado de cierre Fase 4E.2

- WPF local restaurado a `UseVentasApiWrite=false`.
- POS.Api local restaurado a `EnableVentasApiWrite=false`.
- Valores versionados por defecto permanecen en `false`.
