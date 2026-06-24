using PulperiaPOS.DataAccess;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace PulperiaPOS
{
    public partial class IngresoCajaPage : Window
    {
        public IngresoCajaPage()
        {
            InitializeComponent();
            CargarIngresos();
            CalcularDineroEnCaja();
        }

        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void RegistrarIngreso_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(txtMontoIngreso.Text, out double monto))
            {
                MessageBox.Show("Ingrese un monto válido.");
                return;
            }

            string motivo = txtMotivoIngreso.Text.Trim();
            string fecha = DateTime.Now.ToString("yyyy-MM-dd");
            string hora = DateTime.Now.ToString("HH:mm:ss");
            string usuario = UserSession.NombreUsuario ?? "Desconocido";

            try
            {
                using var connection = DBConnection.GetConnection();
                string query = @"INSERT INTO ingreso_caja (monto, motivo, fecha, hora, usuario)
                                 VALUES (@monto, @motivo, @fecha, @hora, @usuario)";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@monto", monto);
                command.Parameters.AddWithValue("@motivo", motivo);
                command.Parameters.AddWithValue("@fecha", fecha);
                command.Parameters.AddWithValue("@hora", hora);
                command.Parameters.AddWithValue("@usuario", usuario);
                command.ExecuteNonQuery();

                RawPrinterHelper.AbrirCajaDesdePOS58();
                MessageBox.Show("Ingreso registrado correctamente.");
                txtMontoIngreso.Clear();
                txtMotivoIngreso.Clear();
                CargarIngresos();
                CalcularDineroEnCaja();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al registrar ingreso: " + ex.Message);
            }
        }

        private void CargarIngresos()
        {
            try
            {
                using var connection = DBConnection.GetConnection();
                string query = "SELECT * FROM ingreso_caja ORDER BY fecha DESC, hora DESC";
                using var adapter = new SqlDataAdapter(query, connection);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);
                dgIngresos.ItemsSource = dataTable.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar ingresos: " + ex.Message);
            }
        }

        private void CalcularDineroEnCaja()
        {
            try
            {
                double totalCaja = CajaHelper.ObtenerDineroAcumuladoCajaChica();
                txtTotalCaja.Text = $"₡{totalCaja:N2}";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al calcular dinero en caja: " + ex.Message);
            }
        }


        private void txtTotalCaja_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
