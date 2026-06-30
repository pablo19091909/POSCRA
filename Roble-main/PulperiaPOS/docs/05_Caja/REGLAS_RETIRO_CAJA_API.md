# Reglas RetiroCaja API

Fecha UTC: 2026-06-29 16:07:09 UTC

## Diferencia con retiro historico

Retiro historico:

- escribe en `retiro_caja`;
- calcula disponible con agregados historicos;
- usa fecha local;
- no tiene turno;
- no guarda usuario en tabla;
- no tiene idempotencia;
- valida fuera de transaccion.

Retiro API futuro:

- escribe solo en `movimiento_caja`;
- usa `caja_turno` abierto;
- usa usuario autenticado;
- usa UTC del servidor/base;
- usa `decimal`;
- calcula efectivo disponible dentro de la misma transaccion;
- usa idempotencia persistente;
- no modifica historicos.

## Efectivo disponible

Formula futura:

```text
 FondoInicial
+ IngresoCaja
+ VentaEfectivo
+ AjustePositivo
- RetiroCaja
- AjusteNegativo
- DevolucionEfectivo
/- Reversa segun tipo revertido
```

Reglas:

- calcular desde `movimiento_caja`;
- filtrar por `idTurno`;
- considerar solo movimientos validos y confirmados;
- no usar `ventas`, `ingreso_caja`, `retiro_caja` ni `cierre_caja`;
- no usar fecha local;
- no aceptar disponible enviado por cliente;
- no usar `float` ni `double`;
- rechazar monto `<= 0`;
- rechazar monto superior al disponible;
- rechazar turno inexistente;
- rechazar turno no `Abierto`;
- rechazar caja logica distinta.

Estado Test actual:

```text
FondoInicial: 1000.00
IngresoCaja: 501.00
RetiroCaja: 0.00
EfectivoDisponible: 1501.00
```

## Contrato futuro

`POST /api/caja/retiros`

Header:

```text
Idempotency-Key: GUID
```

Body permitido:

```text
cajaCodigo
monto
motivo
referencia
```

Body no permitido:

- usuario;
- idTurno;
- fecha UTC;
- estado;
- efectivo disponible;
- request hash;
- idMovimiento;
- datos de cierre.

## Estados y correcciones

- Un retiro confirmado no se edita.
- Un retiro confirmado no se borra.
- Una correccion futura debe usar reversa.
- No se debe crear retiro sobre turno `EnCierre`, `Cerrado` o `Anulado`.
