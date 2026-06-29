# Contrato header Idempotency-Key ingresos

## Header

```text
Idempotency-Key: <GUID>
```

## Validacion

Cuando la escritura este habilitada:

- header requerido;
- formato GUID;
- no `Guid.Empty`;
- una key por intencion;
- no body field;
- no valores compuestos;
- no log de la key.

Con `EnableCajaApiWrite=false`, la API responde `503` antes de validar la key.

## Hash canonico

El hash SHA-256 binario de 32 bytes incluye:

- operacion fija `IngresoCaja`;
- usuario autenticado;
- caja normalizada;
- monto decimal en formato invariante;
- motivo normalizado;
- referencia normalizada.

No incluye:

- timestamps;
- row version;
- token;
- campos derivados;
- datos de servidor no controlados por el cliente.

## Campos sensibles a cambio de intencion

Cambian el hash:

- usuario;
- caja;
- monto;
- motivo;
- referencia.

El cambio de cualquiera de esos campos con la misma key debe responder `409`.
