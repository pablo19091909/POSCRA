using System;

namespace PulperiaPOS.Models.Ventas
{
    public sealed class ReversarVentaRequest
    {
        public Guid? IdempotencyKey { get; set; }
        public string? Motivo { get; set; }
    }
}
