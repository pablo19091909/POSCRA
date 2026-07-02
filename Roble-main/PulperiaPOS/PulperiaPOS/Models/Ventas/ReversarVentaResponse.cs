using System;

namespace PulperiaPOS.Models.Ventas
{
    public sealed class ReversarVentaResponse
    {
        public string Estado { get; set; } = string.Empty;
        public int Factura { get; set; }
        public decimal Monto { get; set; }
        public DateTimeOffset FechaHoraUtc { get; set; }
        public string ResultadoIdempotencia { get; set; } = string.Empty;
    }
}
