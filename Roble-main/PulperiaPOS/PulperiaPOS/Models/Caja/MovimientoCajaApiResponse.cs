using System;

namespace PulperiaPOS.Models.Caja
{
    public sealed class MovimientoCajaApiResponse
    {
        public long IdMovimiento { get; init; }
        public long IdTurno { get; init; }
        public string TipoMovimiento { get; init; } = string.Empty;
        public string Origen { get; init; } = string.Empty;
        public decimal Monto { get; init; }
        public string Moneda { get; init; } = string.Empty;
        public DateTimeOffset FechaHoraUtc { get; init; }
        public int UsuarioId { get; init; }
        public int? Factura { get; init; }
        public long? PagoId { get; init; }
        public int? IngresoCajaId { get; init; }
        public int? RetiroCajaId { get; init; }
        public string? Referencia { get; init; }
        public string? Observacion { get; init; }
        public string Estado { get; init; } = string.Empty;
        public long? ReversaDeMovimientoId { get; init; }
    }
}
