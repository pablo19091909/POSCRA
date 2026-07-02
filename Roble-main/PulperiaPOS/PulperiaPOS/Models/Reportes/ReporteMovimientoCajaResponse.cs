using System;

namespace PulperiaPOS.Models.Reportes
{
    public sealed class ReporteMovimientoCajaResponse
    {
        public string TipoMovimiento { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string Moneda { get; set; } = string.Empty;
        public DateTime FechaUtc { get; set; }
        public string Origen { get; set; } = string.Empty;
        public int? Factura { get; set; }
    }
}

