namespace PulperiaPOS.Models.Caja
{
    public sealed class CajaIngresoRequest
    {
        public string CajaCodigo { get; init; } = string.Empty;
        public decimal Monto { get; init; }
        public string Motivo { get; init; } = string.Empty;
        public string? Referencia { get; init; }
    }
}
