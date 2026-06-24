using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using PulperiaPOS.DataAccess;
using System.Windows.Media;


namespace PulperiaPOS
{
    public partial class ProductoForm : Window
    {
        private Producto productoEditado;

        public ProductoForm(Producto producto = null)
        {
            InitializeComponent();

            if (producto != null)
            {
                productoEditado = producto;
                txtProductoID.Text = producto.idProducto;
                txtProductoID.IsEnabled = false;
                txtNombre.Text = producto.nombre;
                txtProveedor.Text = producto.proveedor;
                txtCosto.Text = producto.costo.ToString("F2");
                txtPrecio.Text = producto.precio.ToString("F2");
                txtStock.Text = producto.stock.ToString();
                txtVendido.Text = producto.vendido.ToString();

                if (producto.costo > 0)
                {
                    double gananciaCalculada = ((double)(producto.precio - producto.costo) / (double)producto.costo) * 100;
                    int gananciaRedondeada = (int)Math.Round(gananciaCalculada);

                    lblGananciaSeleccionada.Text = $"{gananciaCalculada:N1}%";
                    int[] valoresPermitidos = { 30, 35, 40, 45, 50 };

                    if (valoresPermitidos.Contains(gananciaRedondeada))
                    {
                        sliderGanancia.Value = gananciaRedondeada;
                        sliderGanancia.IsEnabled = true;
                        lblGananciaSeleccionada.Foreground = Brushes.White;
                    }
                    else
                    {
                        // Fuera de rango permitido → resaltamos y desactivamos el slider
                        sliderGanancia.IsEnabled = false;
                        chkPrecioManual.IsChecked = true;
                        txtPrecio.IsReadOnly = false;
                        lblGananciaSeleccionada.Text += " (personalizado)";
                        lblGananciaSeleccionada.Foreground = Brushes.Red;
                    }
                }
                else
                {
                    sliderGanancia.Value = 30;
                    lblGananciaSeleccionada.Text = "30%";
                    lblGananciaSeleccionada.Foreground = Brushes.White;
                }
            }
            else
            {
                sliderGanancia.Value = 30;
                lblGananciaSeleccionada.Text = "30%";
                lblGananciaSeleccionada.Foreground = Brushes.White;
            }

            // Eventos
            chkRedondear.Checked += (s, e) => ActualizarPrecio();
            chkRedondear.Unchecked += (s, e) => ActualizarPrecio();

            chkPrecioManual.Checked += (s, e) =>
            {
                txtPrecio.IsReadOnly = false;
                sliderGanancia.IsEnabled = false;
            };
            chkPrecioManual.Unchecked += (s, e) =>
            {
                txtPrecio.IsReadOnly = true;
                sliderGanancia.IsEnabled = true;
                ActualizarPrecio();
            };

            txtPrecio.IsReadOnly = true;
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(txtCosto.Text, out decimal costo) ||
                !decimal.TryParse(txtPrecio.Text, out decimal precio) ||
                !int.TryParse(txtStock.Text, out int stock))
            {
                MessageBox.Show("Asegúrese de ingresar valores numéricos válidos para Costo, Precio y Stock.",
                    "Error de formato", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int vendido = 0;
            if (!string.IsNullOrWhiteSpace(txtVendido.Text) &&
                !int.TryParse(txtVendido.Text, out vendido))
            {
                MessageBox.Show("El campo 'Vendido' debe ser un número entero.", "Error de formato",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var conn = DBConnection.GetConnection();

            if (productoEditado == null)
            {
                var checkID = new SqlCommand("SELECT COUNT(*) FROM inventario WHERE idProducto = @id", conn);
                checkID.Parameters.AddWithValue("@id", txtProductoID.Text.Trim());
                int existeID = Convert.ToInt32(checkID.ExecuteScalar());
                if (existeID > 0)
                {
                    MessageBox.Show("Ya existe un producto con ese ID (Código de Barras).", "Duplicado",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var cmd = new SqlCommand(@"INSERT INTO inventario 
                (idProducto, nombre, proveedor, costo, precio, stock, vendido)
                VALUES (@id, @n, @p, @c, @pr, @s, @v)", conn);
                cmd.Parameters.AddWithValue("@id", txtProductoID.Text.Trim());
                cmd.Parameters.AddWithValue("@n", txtNombre.Text.Trim());
                cmd.Parameters.AddWithValue("@p", txtProveedor.Text.Trim());
                cmd.Parameters.AddWithValue("@c", costo);
                cmd.Parameters.AddWithValue("@pr", precio);
                cmd.Parameters.AddWithValue("@s", stock);
                cmd.Parameters.AddWithValue("@v", vendido);
                cmd.ExecuteNonQuery();
            }
            else
            {
                var cmd = new SqlCommand(@"UPDATE inventario 
                SET nombre = @n, proveedor = @p, costo = @c, precio = @pr, stock = @s, vendido = @v 
                WHERE idProducto = @id", conn);
                cmd.Parameters.AddWithValue("@n", txtNombre.Text.Trim());
                cmd.Parameters.AddWithValue("@p", txtProveedor.Text.Trim());
                cmd.Parameters.AddWithValue("@c", costo);
                cmd.Parameters.AddWithValue("@pr", precio);
                cmd.Parameters.AddWithValue("@s", stock);
                cmd.Parameters.AddWithValue("@v", vendido);
                cmd.Parameters.AddWithValue("@id", productoEditado.idProducto);
                cmd.ExecuteNonQuery();
            }

            MessageBox.Show("Producto guardado correctamente.");
            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void sliderGanancia_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (lblGananciaSeleccionada == null || txtCosto == null || txtPrecio == null)
                return;

            int ganancia = (int)sliderGanancia.Value;
            lblGananciaSeleccionada.Text = $"{ganancia}%";

            if (chkPrecioManual.IsChecked != true && decimal.TryParse(txtCosto.Text, out decimal costo))
            {
                decimal precio = costo + (costo * ganancia / 100);

                if (chkRedondear.IsChecked == true)
                    precio = Math.Ceiling(precio / 25) * 25;

                txtPrecio.Text = precio.ToString("F2");
            }
        }

        private void txtCosto_TextChanged(object sender, TextChangedEventArgs e)
        {
            ActualizarPrecio();
        }

        private void ActualizarPrecio()
        {
            if (chkPrecioManual.IsChecked == true)
                return;

            if (decimal.TryParse(txtCosto.Text, out decimal costo))
            {
                decimal porcentaje = (decimal)sliderGanancia.Value / 100;
                decimal precioCalculado = costo * (1 + porcentaje);

                if (chkRedondear.IsChecked == true)
                    precioCalculado = Math.Ceiling(precioCalculado / 25) * 25;

                txtPrecio.Text = precioCalculado.ToString("F2");
            }
            else
            {
                txtPrecio.Text = "";
            }
        }
    }
}
