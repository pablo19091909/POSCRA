using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PantallaPublicaApp
{
    public class Producto
    {
       
            public string IdProducto { get; set; }
            public string Nombre { get; set; }
            public double Precio { get; set; }     // ← era PrecioVenta, ahora es Precio
            public int Stock { get; set; }         // ← era Cantidad, ahora es Stock
            public bool Seleccionado { get; set; }
        

    }
}
