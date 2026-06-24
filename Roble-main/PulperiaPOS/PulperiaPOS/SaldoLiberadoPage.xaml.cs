using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PulperiaPOS.DataAccess;

namespace PulperiaPOS
{
    public partial class SaldoLiberadoPage : Window
    {
        public SaldoLiberadoPage()
        {
            InitializeComponent();
            CargarLiberaciones();
        }

        private void CargarLiberaciones(string filtroCliente = "", DateTime? filtroFecha = null)
        {
            try
            {
                using var conn = DBConnection.GetConnection();

                string query = @"
                    SELECT l.idLiberacion, c.nombre AS nombreCliente, l.monto, l.fecha, l.motivo
                    FROM saldo_liberado l
                    JOIN cliente c ON c.idCliente = l.idCliente
                    WHERE 1=1";

                if (!string.IsNullOrWhiteSpace(filtroCliente))
                    query += " AND (LOWER(c.nombre) LIKE @cliente OR c.idCliente LIKE @clienteExacto)";

                if (filtroFecha != null)
                    query += " AND l.fecha = @fecha";

                query += " ORDER BY l.fecha DESC";

                using var cmd = new SqlCommand(query, conn);

                if (!string.IsNullOrWhiteSpace(filtroCliente))
                {
                    cmd.Parameters.AddWithValue("@cliente", $"%{filtroCliente.ToLower()}%");
                    cmd.Parameters.AddWithValue("@clienteExacto", filtroCliente);
                }

                if (filtroFecha != null)
                    cmd.Parameters.AddWithValue("@fecha", filtroFecha.Value.ToString("yyyy-MM-dd"));

                using var reader = cmd.ExecuteReader();
                var lista = new List<dynamic>();

                while (reader.Read())
                {
                    lista.Add(new
                    {
                        idLiberacion = reader.GetInt32(0),
                        nombreCliente = reader.GetString(1),
                        monto = Convert.ToDecimal(reader["monto"]),
                        fecha = reader.GetDateTime(3).ToString("yyyy-MM-dd"), // ✅ corrección aquí
                        motivo = reader.IsDBNull(4) ? "" : reader.GetString(4)
                    });
                }

                dataGridLiberaciones.ItemsSource = lista;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar historial: " + ex.Message);
            }
        }

        private void BtnFiltrar_Click(object sender, RoutedEventArgs e)
        {
            string nombre = txtBuscarCliente.Text.Trim();
            DateTime? fecha = dpFecha.SelectedDate;
            CargarLiberaciones(nombre, fecha);
        }

        private void BtnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            txtBuscarCliente.Clear();
            dpFecha.SelectedDate = null;
            CargarLiberaciones();
        }

        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
