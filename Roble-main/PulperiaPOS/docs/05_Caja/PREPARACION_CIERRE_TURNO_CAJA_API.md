# Preparacion para cierre de turno Caja API

Fecha UTC: 2026-06-29 21:02:52 UTC

## Listo para iniciar diseno de cierre

- Turno unico y controlado para `CAJA_PRINCIPAL_TEST`.
- Movimientos inmutables validados por lectura.
- Pre-cierre por turno disponible y consistente.
- Ingresos y retiros idempotentes.
- Control de concurrencia validado.
- Proteccion por ambiente `Environment=Test`.
- Aislamiento historico confirmado para `ingreso_caja`, `retiro_caja` y `cierre_caja`.
- Health checks de API disponibles.
- Rutas de lectura protegidas por `Caja.Ver`.

## Pendiente antes de cierre real

- Transicion transaccional de `Abierto` a `EnCierre`.
- Uso obligatorio de `row_version`.
- Captura de efectivo contado.
- Calculo de diferencia.
- Requerimiento de observacion cuando exista diferencia.
- Idempotencia para `CerrarTurno`.
- Impedir ingreso, retiro y venta en efectivo durante `EnCierre` o `Cerrado`.
- Movimiento `CierreDiferencia`, si corresponde.
- Reversas.
- Integracion de ventas en efectivo con `MovimientoCaja`.
- Migracion futura de WPF a Caja API.

## Limites recomendados para Fase 4F.18

La siguiente fase debe ser de diseno y preparacion no ejecutada. No debe cerrar turnos, crear movimientos, escribir idempotencias ni modificar WPF.

## Recomendacion

Continuar con Fase 4F.18: diseno tecnico del cierre de turno API, definiendo contrato, permisos, validaciones, idempotencia y estrategia transaccional antes de cualquier escritura.
