using PulperiaPOS.DataAccess;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace PulperiaPOS
{
    public partial class InventarioWindow : Window
    {
        public InventarioWindow()
        {
            InitializeComponent();
            CargarInventario();
            txtBuscar.Foreground = System.Windows.Media.Brushes.Gray;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (UserSession.RolUsuario == "Anfitrion")
            {
                BtnAgregar.Visibility = Visibility.Collapsed;
                foreach (var col in dgInventario.Columns)
                {
                    if (col.Header?.ToString() == "Acciones")
                    {
                        col.Visibility = Visibility.Collapsed;
                        break;
                    }
                }
            }
        }

        private void CargarInventario(string filtro = "")
        {
            List<Producto> lista = new List<Producto>();
            using var conn = DBConnection.GetConnection();
            string query = "SELECT * FROM inventario";

            if (!string.IsNullOrWhiteSpace(filtro))
                query += " WHERE idProducto LIKE @filtro OR nombre LIKE @filtro";

            using var cmd = new SqlCommand(query, conn);
            if (!string.IsNullOrWhiteSpace(filtro))
                cmd.Parameters.AddWithValue("@filtro", $"%{filtro}%");

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new Producto
                {
                    idProducto = reader.GetString(0),
                    nombre = reader.GetString(1),
                    proveedor = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    costo = Convert.ToDouble(reader.GetDecimal(3)),
                    precio = Convert.ToDouble(reader.GetDecimal(4)),
                    stock = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                    vendido = reader.IsDBNull(6) ? 0 : reader.GetInt32(6)
                });
            }

            dgInventario.ItemsSource = lista;
        }

        private void BtnBuscar_Click(object sender, RoutedEventArgs e)
        {
            CargarInventario(txtBuscar.Text);
        }
        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtBuscar.Text != "Buscar producto por ID o Nombre")
            {
                CargarInventario(txtBuscar.Text.Trim());
            }
        }

        private void BtnAgregar_Click(object sender, RoutedEventArgs e)
        {
            var ventanaAgregar = new ProductoForm();
            ventanaAgregar.ShowDialog();
            CargarInventario();
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            if (dgInventario.SelectedItem is Producto producto)
            {
                var ventanaEditar = new ProductoForm(producto);
                ventanaEditar.ShowDialog();
                CargarInventario();
            }
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (dgInventario.SelectedItem is Producto producto)
            {
                var result = MessageBox.Show("¿Desea eliminar este producto?", "Confirmar", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    using var conn = DBConnection.GetConnection();
                    using var cmd = new SqlCommand("DELETE FROM inventario WHERE idProducto = @id", conn);
                    cmd.Parameters.AddWithValue("@id", producto.idProducto);
                    cmd.ExecuteNonQuery();

                    CargarInventario();
                }
                MessageBox.Show("Producto Eliminado correctamente.");
            }
        }

        private void txtBuscar_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtBuscar.Text == "Buscar producto por ID o Nombre")
            {
                txtBuscar.Text = "";
                txtBuscar.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void txtBuscar_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBuscar.Text))
            {
                txtBuscar.Text = "Buscar producto por ID o Nombre";
                txtBuscar.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void BtnRefrescar_Click(object sender, RoutedEventArgs e)
        {
            CargarInventario();
        }

        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnExportarPDF_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var conn = DBConnection.GetConnection();
                using var cmd = new SqlCommand("SELECT * FROM inventario ORDER BY stock ASC", conn);
                using var reader = cmd.ExecuteReader();

                List<Producto> productos = new();
                while (reader.Read())
                {
                    productos.Add(new Producto
                    {
                        idProducto = reader.GetString(0),
                        nombre = reader.GetString(1),
                        proveedor = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        costo = Convert.ToDouble(reader.GetDecimal(3)),
                        precio = Convert.ToDouble(reader.GetDecimal(4)),
                        stock = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                        vendido = reader.IsDBNull(6) ? 0 : reader.GetInt32(6)
                    });
                }

                GenerarPDFInventario(productos);
                MessageBox.Show("PDF generado correctamente.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar PDF: " + ex.Message);
            }
        }
        

        private void GenerarPDFInventario(List<Producto> productos)
        {
            try
            {
                PdfDocument pdf = new PdfDocument();
                pdf.Info.Title = "Reporte de Inventario";

                PdfPage page = pdf.AddPage();
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XFont font = new XFont("Arial", 10, XFontStyle.Regular);
                XFont fontBold = new XFont("Arial", 10, XFontStyle.Bold);

                double yPoint = 40;
                string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "logo.png");
                if (File.Exists(logoPath))
                {
                    using var stream = new FileStream(logoPath, FileMode.Open, FileAccess.Read);
                    XImage logo = XImage.FromStream(() => stream);
                    gfx.DrawImage(logo, page.Width - 80, 10, 60, 60);
                }

                yPoint += 20;
                gfx.DrawString("REPORTE DE INVENTARIO", fontBold, XBrushes.Black, new XRect(0, yPoint, page.Width, 40), XStringFormats.TopCenter);
                yPoint += 40;

                gfx.DrawString("ID", fontBold, XBrushes.Black, 30, yPoint);
                gfx.DrawString("Nombre", fontBold, XBrushes.Black, 100, yPoint);
                gfx.DrawString("Proveedor", fontBold, XBrushes.Black, 250, yPoint);
                gfx.DrawString("Stock", fontBold, XBrushes.Black, 360, yPoint);
                gfx.DrawString("Vendido", fontBold, XBrushes.Black, 420, yPoint);
                yPoint += 25;

                foreach (var p in productos.OrderBy(p => p.stock))
                {
                    var color = p.stock <= 5 ? XBrushes.Red : XBrushes.Black;
                    gfx.DrawString(p.idProducto, font, color, 30, yPoint);
                    gfx.DrawString(p.nombre, font, color, 100, yPoint);
                    gfx.DrawString(p.proveedor, font, color, 250, yPoint);
                    gfx.DrawString(p.stock.ToString(), font, color, 360, yPoint);
                    gfx.DrawString(p.vendido.ToString(), font, color, 420, yPoint);

                    yPoint += 20;
                    if (yPoint > page.Height - 50)
                    {
                        page = pdf.AddPage();
                        gfx = XGraphics.FromPdfPage(page);
                        yPoint = 40;
                    }
                }

                string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Inventario_Report.pdf");
                pdf.Save(filePath);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar el PDF: " + ex.Message);
            }
        }

        
    }

    public class Producto
    {
        public string idProducto { get; set; }
        public string nombre { get; set; }
        public string proveedor { get; set; }
        public double costo { get; set; }
        public double precio { get; set; }
        public int stock { get; set; }
        public int vendido { get; set; }
    }
}
