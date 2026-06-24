using PulperiaPOS.DataAccess;
using PulperiaPOS.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Drawing.Printing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

// ... resto de using igual ...
namespace PulperiaPOS
{
    public partial class DonacionesPage : Window
    {
        private ObservableCollection<ProductoVenta> productosDonacion = new();

        public DonacionesPage()
        {
            InitializeComponent();
            ActualizarTotalDonacion();
            this.Loaded += DonacionesPage_Loaded;
        }

        private void DonacionesPage_Loaded(object sender, RoutedEventArgs e)
        {
            BuscarProductoTextBox.Focus();
        }

        private void BuscarProductoTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AgregarProductoPorCodigo();
                e.Handled = true;
            }
        }

        private void AgregarProducto_Click(object sender, RoutedEventArgs e)
        {
            AgregarProductoPorCodigo();
        }

        private void AgregarProductoPorCodigo()
        {
            string textoBusqueda = BuscarProductoTextBox.Text.Trim();
            if (string.IsNullOrEmpty(textoBusqueda)) return;

            try
            {
                using var connection = DBConnection.GetConnection();
                string query = "SELECT idProducto, nombre, precio, stock FROM inventario WHERE idProducto = @busqueda OR nombre LIKE @busquedaNombre";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@busqueda", textoBusqueda);
                command.Parameters.AddWithValue("@busquedaNombre", $"%{textoBusqueda}%");

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    int stock = Convert.ToInt32(reader["stock"]);
                    if (stock <= 0)
                    {
                        MessageBox.Show("El producto no tiene stock disponible.");
                        return;
                    }

                    string idProducto = reader["idProducto"].ToString();
                    var productoExistente = productosDonacion.FirstOrDefault(p => p.IdProducto == idProducto);
                    if (productoExistente != null)
                    {
                        if (productoExistente.Cantidad + 1 > stock)
                        {
                            MessageBox.Show("No hay suficiente stock disponible.");
                            return;
                        }
                        productoExistente.Cantidad += 1;
                    }
                    else
                    {
                        productosDonacion.Add(new ProductoVenta
                        {
                            IdProducto = idProducto,
                            Nombre = reader["nombre"].ToString(),
                            PrecioUnitario = Convert.ToDouble(reader["precio"]),
                            Cantidad = 1,
                            StockDisponible = stock
                        });
                    }

                    ProductosDataGrid.ItemsSource = null;
                    ProductosDataGrid.ItemsSource = productosDonacion;
                    ActualizarTotalDonacion();
                    BuscarProductoTextBox.Clear();
                    BuscarProductoTextBox.Focus();
                }
                else
                {
                    MessageBox.Show("Producto no encontrado.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al buscar el producto: " + ex.Message);
            }
        }

        private void ActualizarTotalDonacion()
        {
            double total = productosDonacion.Sum(p => p.Subtotal);
            TotalDonacionTextBlock.Text = $"₡{total:N2}";
        }

        private void RegistrarDonacion_Click(object sender, RoutedEventArgs e)
        {
            if (productosDonacion.Count == 0)
            {
                MessageBox.Show("Agregue al menos un producto para donar.");
                return;
            }

            string motivo = MotivoTextBox.Text.Trim();
            if (string.IsNullOrEmpty(motivo))
            {
                MessageBox.Show("Debe indicar un motivo para la donación.");
                return;
            }

            int idClienteGeneral = 0; // Ya que ahora tienes el Cliente General con ID 0
            double total = productosDonacion.Sum(p => p.Subtotal);

            try
            {
                using var connection = DBConnection.GetConnection();
                using var transaction = connection.BeginTransaction();

                string insertVenta = @"
    INSERT INTO ventas
    (cliente_id, total, fecha, hora, usuario_id, metodo_pago, numero_comprobante, monto_pagado, vuelto)
    VALUES
    (@cliente_id, @total, @fecha, @hora, @usuario_id, @metodo_pago, @numero_comprobante, 0, 0);
    SELECT SCOPE_IDENTITY();";

                using var cmd = new SqlCommand(insertVenta, connection, transaction);
                cmd.Parameters.AddWithValue("@cliente_id", idClienteGeneral);
                cmd.Parameters.AddWithValue("@total", total);
                cmd.Parameters.AddWithValue("@fecha", DateTime.Now.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@hora", DateTime.Now.ToString("HH:mm:ss"));
                cmd.Parameters.AddWithValue("@usuario_id", UserSession.IdUsuario);
                cmd.Parameters.AddWithValue("@metodo_pago", "Donación");
                cmd.Parameters.AddWithValue("@numero_comprobante", motivo);

                object result = cmd.ExecuteScalar();

                if (result == null || result == DBNull.Value)
                {
                    throw new Exception("No se pudo obtener el ID de la venta recién insertada.");
                }

                long facturaId = Convert.ToInt64(result);


                foreach (var producto in productosDonacion)
                {
                    string insertDetalle = @"
                INSERT INTO DetalleVenta (factura, producto_id, cantidad, precio_unitario)
                VALUES (@factura, @producto_id, @cantidad, @precio_unitario)";
                    using var detalleCmd = new SqlCommand(insertDetalle, connection, transaction);
                    detalleCmd.Parameters.AddWithValue("@factura", facturaId);
                    detalleCmd.Parameters.AddWithValue("@producto_id", producto.IdProducto);
                    detalleCmd.Parameters.AddWithValue("@cantidad", producto.Cantidad);
                    detalleCmd.Parameters.AddWithValue("@precio_unitario", producto.PrecioUnitario);
                    detalleCmd.ExecuteNonQuery();

                    string updateStock = @"
                UPDATE inventario
                SET stock = stock - @cantidad
                WHERE idProducto = @producto_id";
                    using var stockCmd = new SqlCommand(updateStock, connection, transaction);
                    stockCmd.Parameters.AddWithValue("@cantidad", producto.Cantidad);
                    stockCmd.Parameters.AddWithValue("@producto_id", producto.IdProducto);
                    stockCmd.ExecuteNonQuery();
                }

                transaction.Commit();

                MessageBox.Show("Donación registrada exitosamente.");
                productosDonacion.Clear();
                ProductosDataGrid.ItemsSource = null;
                ProductosDataGrid.ItemsSource = productosDonacion;
                ActualizarTotalDonacion();
                MotivoTextBox.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al registrar la donación: " + ex.Message);
            }
        }


        private void EliminarProducto_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ProductoVenta producto)
            {
                productosDonacion.Remove(producto);
                ProductosDataGrid.ItemsSource = null;
                ProductosDataGrid.ItemsSource = productosDonacion;
                ActualizarTotalDonacion();
            }
        }

        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ProductosDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            ActualizarTotalDonacion();
        }
    }

    public class ProductoVenta
    {
        public string IdProducto { get; set; }
        public string Nombre { get; set; }
        public int Cantidad { get; set; }
        public double PrecioUnitario { get; set; }
        public int StockDisponible { get; set; }
        public double Subtotal => Cantidad * PrecioUnitario;
    }
}

