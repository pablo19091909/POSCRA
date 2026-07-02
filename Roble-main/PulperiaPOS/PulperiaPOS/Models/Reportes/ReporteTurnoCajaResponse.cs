using System;

namespace PulperiaPOS.Models.Reportes
{
    public sealed class ReporteTurnoCajaResponse
    {
        public string CajaCodigo { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime AperturaUtc { get; set; }
        public DateTime? CierreUtc { get; set; }
        public decimal FondoInicial { get; set; }
        public decimal EfectivoEsperado { get; set; }
        public decimal? EfectivoContado { get; set; }
        public decimal? Diferencia { get; set; }
        public string Fuente { get; set; } = string.Empty;
    }
}

