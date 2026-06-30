namespace PulperiaPOS.Models.Caja
{
    public sealed class ResumenMovimientoCajaApiResponse
    {
        public string TipoMovimiento { get; init; } = string.Empty;
        public int Cantidad { get; init; }
        public decimal Total { get; init; }
    }
}
