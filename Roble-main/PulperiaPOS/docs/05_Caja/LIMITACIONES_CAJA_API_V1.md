# Limitaciones Caja API V1

- Escritura bloqueada por `EnableCajaApiWrite=false`.
- No se crean turnos.
- No se crean movimientos.
- No se conecto WPF.
- No se integraron ventas API con caja.
- No hay idempotencia especifica de caja.
- No hay endpoints activos de anulacion o devolucion.
- No hay conciliacion tarjeta/SINPE.
- Dolares y Donacion siguen fuera de alcance.
- Caja historica WPF sigue separada.
- Reportes deben distinguir caja historica y caja API por turno.
