namespace PulperiaPOS.Models.Reportes
{
    public sealed class ReporteCajaResumenResponse
    {
        public int TurnosAbiertos { get; set; }
        public int TurnosEnCierre { get; set; }
        public int TurnosCerrados { get; set; }
        public decimal FondoInicial { get; set; }
        public decimal Ingresos { get; set; }
        public decimal Retiros { get; set; }
        public decimal VentaEfectivo { get; set; }
        public decimal Reversas { get; set; }
        public decimal CierreDiferencia { get; set; }
        public decimal EfectivoEsperadoCalculado { get; set; }
        public decimal EfectivoContadoCerrado { get; set; }
        public decimal DiferenciaCerrada { get; set; }
        public string Fuente { get; set; } = string.Empty;
    }
}

