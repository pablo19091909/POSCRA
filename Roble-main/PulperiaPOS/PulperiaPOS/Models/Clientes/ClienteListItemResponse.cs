using System;

namespace PulperiaPOS.Models.Clientes
{
    public sealed class ClienteListItemResponse
    {
        public int IdCliente { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal Saldo { get; set; }
        public string Comprobante { get; set; } = string.Empty;
        public DateTimeOffset? FechaCargaSaldoUtc { get; set; }
    }
}
