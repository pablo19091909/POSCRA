using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using PulperiaPOS.DataAccess;

namespace PulperiaPOS.Views
{
    /// <summary>
    /// Lógica de interacción para DetalleVentaWindow.xaml
    /// </summary>
    public partial class DetalleVentaWindow : Window
    {
        private int facturaId;

        public DetalleVentaWindow(int factura)
        {
            InitializeComponent();
            facturaId = factura;
            CargarDetalleVenta();
        }

        private void CargarDetalleVenta()
        {
            try
            {
                using (var connection = DBConnection.GetConnection())
                {
                    string query = @"
                        SELECT 
                            dv.idDetalle, 
                            dv.producto_id, 
                            i.nombre AS nombre_producto,
                            dv.cantidad, 
                            dv.precio_unitario,
                            (dv.cantidad * dv.precio_unitario) AS subtotal
                        FROM DetalleVenta dv
                        JOIN inventario i ON dv.producto_id = i.idProducto
                        WHERE dv.factura = @facturaId";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@facturaId", facturaId);
                        var adapter = new SqlDataAdapter(command);
                        var table = new DataTable();
                        adapter.Fill(table);

                        decimal totalVenta = 0;

                        foreach (DataRow row in table.Rows)
                        {
                            if (row["subtotal"] != DBNull.Value)
                                totalVenta += Convert.ToDecimal(row["subtotal"]);
                        }

                        var filaTotal = table.NewRow();
                        filaTotal["nombre_producto"] = "TOTAL";
                        filaTotal["subtotal"] = totalVenta;
                        table.Rows.Add(filaTotal);

                        DetalleDataGrid.ItemsSource = table.DefaultView;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar detalle: " + ex.Message);
            }
        }

        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
