# Manejo de reintentos e idempotencia WPF

## Principio

WPF genera una `IdempotencyKey` por intencion de venta y la conserva solo en memoria. La intencion incluye cliente, productos, cantidades y pago.

## Ciclo de vida

1. Antes de enviar, WPF calcula la huella de la intencion actual.
2. Si no hay intencion pendiente, genera una nueva key.
3. Si la intencion es igual a la pendiente, reutiliza la misma key.
4. Si la intencion cambia, descarta la pendiente y genera otra key.
5. En exito confirmado, limpia la intencion y el carrito.
6. En error, conserva carrito y permite reintento controlado.

## Timeout o red

Si el resultado es incierto por timeout o red, WPF no limpia el carrito y no genera una venta por SQL. El operador puede reintentar la misma intencion con la misma key.

## 503

Si la API esta deshabilitada o no disponible, WPF muestra un mensaje seguro, no limpia el carrito y no hace fallback SQL.

## 409

Si la API informa conflicto de idempotencia o solicitud en proceso, WPF muestra un mensaje seguro y no crea una venta nueva automaticamente.

## 401

La infraestructura central maneja sesion expirada. WPF no registra por SQL y no limpia automaticamente el carrito.

## Seguridad

La key no se imprime, no se registra, no se documenta con valor real y no se guarda en disco.
