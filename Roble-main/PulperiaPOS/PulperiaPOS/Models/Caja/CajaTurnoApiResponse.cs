using System;

namespace PulperiaPOS.Models.Caja
{
    public sealed class CajaTurnoApiResponse
    {
        public long IdTurno { get; init; }
        public string CajaCodigo { get; init; } = string.Empty;
        public string Estado { get; init; } = string.Empty;
        public int UsuarioAperturaId { get; init; }
        public int? UsuarioCierreId { get; init; }
        public DateTimeOffset AperturaUtc { get; init; }
        public DateTimeOffset? CierreUtc { get; init; }
        public decimal FondoInicial { get; init; }
        public decimal? EfectivoEsperado { get; init; }
        public decimal? EfectivoContado { get; init; }
        public decimal? Diferencia { get; init; }
        public string? ObservacionApertura { get; init; }
        public string? ObservacionCierre { get; init; }
        public int? CierreCajaId { get; init; }
        public string RowVersion { get; init; } = string.Empty;
    }
}
