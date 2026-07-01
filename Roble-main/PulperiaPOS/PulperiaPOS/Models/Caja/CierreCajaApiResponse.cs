using System;
using System.Collections.Generic;

namespace PulperiaPOS.Models.Caja
{
    public sealed class CierreCajaApiResponse
    {
        public long IdTurno { get; init; }
        public string CajaCodigo { get; init; } = string.Empty;
        public string Estado { get; init; } = string.Empty;
        public decimal EfectivoEsperado { get; init; }
        public decimal EfectivoContado { get; init; }
        public decimal Diferencia { get; init; }
        public DateTimeOffset CierreUtc { get; init; }
        public bool CierreDiferenciaCreado { get; init; }
        public IReadOnlyCollection<ResumenMovimientoCajaApiResponse> Resumen { get; init; } = Array.Empty<ResumenMovimientoCajaApiResponse>();
    }
}
