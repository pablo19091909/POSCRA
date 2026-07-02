using System;

namespace PulperiaPOS.Models.Reportes
{
    public sealed class ReporteVentaDetalleResponse
    {
        public int Factura { get; set; }
        public DateTime? Fecha { get; set; }
        public string Estado { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string MetodoPago { get; set; } = string.Empty;
        public string Origen { get; set; } = string.Empty;
        public bool Reversada { get; set; }
        public DateTime? ReversaUtc { get; set; }
        public decimal ImpactoNeto { get; set; }
    }
}

