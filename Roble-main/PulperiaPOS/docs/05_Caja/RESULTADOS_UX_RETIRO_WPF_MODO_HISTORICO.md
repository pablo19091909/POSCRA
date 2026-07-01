# Resultados UX - Retiros WPF en modo historico

Fecha UTC: 2026-06-30

## Resultado general

La pantalla `RetirosCajaPage` fue validada visualmente en modo historico con lectura Caja API activa y escritura de retiro API apagada.

## Elementos visibles confirmados

- Indicador de Caja API visible.
- Estado del turno visible.
- Fondo inicial visible.
- Cantidad de movimientos visible.
- Efectivo esperado visible.
- Resumen por tipo de movimiento visible.
- Aviso de modo historico SQL visible.
- Campo `Dinero en Caja (Calculado)` visible.
- Campos de monto y motivo visibles.
- Boton `Registrar Retiro` visible.

## Comportamiento esperado confirmado

La pantalla permite observar el estado de caja consultado por API sin cambiar todavia el mecanismo historico de registro de retiros.

No se ejecuto ningun registro de retiro.

## Hallazgo UX pendiente

El valor historico `Dinero en Caja (Calculado)` puede diferir del efectivo esperado por Caja API. Esto puede confundir durante la transicion porque la pantalla muestra simultaneamente una lectura API moderna y un calculo historico SQL.

Antes de habilitar retiro API se recomienda ajustar el texto visual para separar claramente:

- Saldo esperado de Caja API.
- Calculo historico SQL usado solo mientras el registro API esta apagado.

## Conclusion

La UX es suficiente para validacion no destructiva de lectura. Para escritura API activa se recomienda una revision visual adicional.
