using System;
using System.Collections.Generic;

namespace PulperiaPOS.Models.Caja
{
    public sealed class PreCierreCajaApiResponse
    {
        public long IdTurno { get; init; }
        public string CajaCodigo { get; init; } = string.Empty;
        public string Estado { get; init; } = string.Empty;
        public DateTimeOffset AperturaUtc { get; init; }
        public decimal EfectivoEsperado { get; init; }
        public string RowVersion { get; init; } = string.Empty;
        public IReadOnlyCollection<ResumenMovimientoCajaApiResponse> Resumen { get; init; } = Array.Empty<ResumenMovimientoCajaApiResponse>();
    }
}
