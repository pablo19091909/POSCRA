namespace PulperiaPOS.Models.Caja
{
    public sealed class AbrirCajaTurnoApiRequest
    {
        public string CajaCodigo { get; init; } = string.Empty;
        public decimal FondoInicial { get; init; }
        public string? Observacion { get; init; }
    }
}
