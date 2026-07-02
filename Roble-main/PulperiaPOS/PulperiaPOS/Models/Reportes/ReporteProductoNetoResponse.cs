namespace PulperiaPOS.Models.Reportes
{
    public sealed class ReporteProductoNetoResponse
    {
        public string Producto { get; set; } = string.Empty;
        public decimal CantidadBruta { get; set; }
        public decimal CantidadRestaurada { get; set; }
        public decimal CantidadNeta { get; set; }
        public decimal VentaNeta { get; set; }
    }
}

