namespace PulperiaPOS.Models.Caja
{
    public sealed class CajaCierreRequest
    {
        public decimal EfectivoContado { get; init; }
        public string? Observacion { get; init; }
        public string RowVersion { get; init; } = string.Empty;
    }
}
