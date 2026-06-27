using System;
using System.Collections.Generic;

namespace PulperiaPOS.Models.Ventas
{
    public sealed record VentaResponse(
        int Factura,
        string Estado,
        decimal Total,
        decimal MontoPagado,
        decimal? Vuelto,
        string MetodoPago,
        DateTimeOffset FechaHoraUtc,
        string ResultadoIdempotencia,
        IReadOnlyCollection<VentaItemResponse> Items,
        PagoVentaResponse Pago);
}
