namespace PulperiaPOS.Models.Reportes
{
    public sealed class ReporteInconsistenciaResponse
    {
        public string Codigo { get; set; } = string.Empty;
        public string Severidad { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public string AccionRecomendada { get; set; } = string.Empty;
    }
}

