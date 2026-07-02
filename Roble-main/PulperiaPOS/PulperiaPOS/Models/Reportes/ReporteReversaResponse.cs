using System;

namespace PulperiaPOS.Models.Reportes
{
    public sealed class ReporteReversaResponse
    {
        public int Factura { get; set; }
        public decimal Monto { get; set; }
        public string Moneda { get; set; } = string.Empty;
        public DateTime FechaUtc { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public bool TieneMovimientoCompensatorio { get; set; }
        public bool Consistente { get; set; }
    }
}

