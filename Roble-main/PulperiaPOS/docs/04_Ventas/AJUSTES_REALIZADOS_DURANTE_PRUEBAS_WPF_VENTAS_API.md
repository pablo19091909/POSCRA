# Ajustes realizados durante pruebas WPF Ventas API

Fecha/hora de cierre: 2026-06-28 11:08 UTC.

Este documento registra los ajustes aplicados durante la validacion manual de Fase 4E.2. No se agregaron nuevas correcciones funcionales durante el cierre formal; solo se documento y verifico el estado final.

## Metodo de pago forzado

- Modulo: `PulperiaPOS/VentasPage.xaml.cs`.
- Problema: al seleccionar clientes de prueba, el metodo de pago podia quedar forzado a `Saldo Cliente`, impidiendo probar efectivo, tarjeta y SINPE.
- Ajuste: en modo API, el metodo de pago soportado queda seleccionable por el operador.
- Riesgo mitigado: pruebas incompletas y ventas enviadas con metodo incorrecto.
- Confirmacion: pruebas manuales de efectivo exacto, efectivo con vuelto, tarjeta y SINPE completadas.

## Saldo insuficiente

- Modulo: `PulperiaPOS/VentasPage.xaml.cs`.
- Problema: el flujo de `Saldo Cliente` no notificaba correctamente saldo insuficiente.
- Ajuste: se agrego validacion visual antes de enviar la venta API.
- Riesgo mitigado: enviar solicitudes que el API debe rechazar sin feedback claro para el operador.
- Confirmacion: cliente sintetico de saldo bajo muestra notificacion segura.

## Cliente General

- Modulos:
  - `PulperiaPOS/VentasPage.xaml.cs`.
  - `POS.Api/Application/Ventas/VentaService.cs`.
  - `POS.Api/Infrastructure/Data/Clientes/ClienteRepository.cs`.
- Problema: Cliente General existe con identificador `0`; WPF/API lo trataban como cliente invalido.
- Ajuste: se permite `ClienteId=0` y se rechaza solo `ClienteId < 0`. El selector prioriza Cliente General.
- Riesgo mitigado: imposibilidad de vender a consumidor no registrado y precarga accidental de un cliente historico.
- Confirmacion: Cliente General completa venta API permitida y no usa saldo de cliente.

## Orden del selector de clientes

- Modulo: `POS.Api/Infrastructure/Data/Clientes/ClienteRepository.cs`.
- Problema: la primera carga del selector podia mostrar un cliente historico como valor inicial.
- Ajuste: Cliente General queda primero en la lista.
- Riesgo mitigado: venta accidental asociada al cliente equivocado.
- Confirmacion: selector inicia con Cliente General de forma coherente.

## Bloqueos V1

- Modulo: `PulperiaPOS/VentasPage.xaml.cs`.
- Comportamiento confirmado: Dolares permanece bloqueado en Venta API V1.
- Comportamiento confirmado: Donacion y pagos combinados no fueron habilitados.

## Resultado de cierre

- No hay duplicados de idempotencia.
- No hay pagos duplicados por factura API.
- No hay stock negativo en productos sinteticos.
- No hay saldo negativo en clientes sinteticos.
- Caja no fue alterada por Venta API.
- Flags de escritura restaurados a `false`.
