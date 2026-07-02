# Plan de activacion gradual de reversas API

## Estado listo

La persistencia, endpoint y UI bloqueada existen. Las escrituras permanecen apagadas.

## Siguiente paso recomendado

Fase 5B.6A manual guiada:

1. iniciar POS.Api;
2. activar temporalmente apertura;
3. abrir turno desde WPF;
4. activar venta efectivo;
5. crear una unica venta desde WPF;
6. activar reversa;
7. reversar la venta desde WPF;
8. activar cierre;
9. cerrar exacto desde WPF;
10. restaurar todos los flags;
11. validar agregados finales.

## Criterios de avance

- cero fallback SQL;
- cero dual write;
- cero impresion historica;
- venta original intacta;
- una sola reversa;
- inventario restaurado una vez;
- efectivo esperado de vuelta a 1000.00;
- turno cerrado exacto.
