using System.Collections.Generic;

namespace PulperiaPOS.Models.Reportes
{
    public sealed class ReporteVentasResumenResponse
    {
        public decimal VentasBrutas { get; set; }
        public decimal MontoReversado { get; set; }
        public decimal VentasNetas { get; set; }
        public int CantidadVentas { get; set; }
        public int CantidadVentasReversadas { get; set; }
        public int CantidadReversas { get; set; }
        public decimal EfectivoVentasBruto { get; set; }
        public decimal ReversasEfectivo { get; set; }
        public decimal EfectivoVentasNeto { get; set; }
        public IReadOnlyCollection<ReporteMetodoPagoTotalResponse> TotalesPorMetodoPago { get; set; } = [];
        public IReadOnlyCollection<ReporteProductoNetoResponse> ProductosNetos { get; set; } = [];
    }
}

