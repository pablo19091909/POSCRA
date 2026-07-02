namespace PulperiaPOS.Models.Reportes
{
    public sealed class ReporteMetodoPagoTotalResponse
    {
        public string MetodoPago { get; set; } = string.Empty;
        public decimal Bruto { get; set; }
        public decimal Reversado { get; set; }
        public decimal Neto { get; set; }
        public int CantidadVentas { get; set; }
    }
}

