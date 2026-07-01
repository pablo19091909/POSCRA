namespace PulperiaPOS.Models.Caja
{
    public sealed class CajaCierreViewModel
    {
        public long IdTurno { get; init; }
        public decimal EfectivoContado { get; init; }
        public string? Observacion { get; init; }
        public string RowVersion { get; init; } = string.Empty;
        public decimal EfectivoEsperadoEstimado { get; init; }
    }
}
