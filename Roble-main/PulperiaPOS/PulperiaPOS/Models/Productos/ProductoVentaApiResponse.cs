namespace PulperiaPOS.Models.Productos
{
    public sealed class ProductoVentaApiResponse
    {
        public string IdProducto { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int StockDisponible { get; set; }
        public bool Disponible { get; set; }
    }
}
