using PulperiaPOS.DataAccess;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing.Printing;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace PulperiaPOS.Views
{
    public partial class RetirosCajaPage : Window
    {
        private double dineroDisponibleEnCaja = 0;

        public RetirosCajaPage()
        {
            InitializeComponent();
            CargarRetiros();
            CalcularDineroEnCaja();
            _ = CajaApiReadStatusViewHelper.LoadAsync(txtCajaApiStatus, nameof(RetirosCajaPage));
        }

        private void RegistrarRetiro_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(txtMontoRetiro.Text, out double monto))
            {
                MessageBox.Show("Ingrese un monto válido.");
                return;
            }

            if (monto > dineroDisponibleEnCaja)
            {
                MessageBox.Show($"❌ El monto a retirar (₡{monto:N2}) excede el efectivo disponible en caja (₡{dineroDisponibleEnCaja:N2}).");
                return;
            }

            string motivo = txtMotivoRetiro.Text.Trim();
            string fecha = DateTime.Now.ToString("yyyy-MM-dd");
            string hora = DateTime.Now.ToString("HH:mm:ss");
            string usuario = UserSession.NombreUsuario ?? "Desconocido";

            // Abrir caja registradora
            RawPrinterHelper.AbrirCajaDesdePOS58();

            // Confirmar con el usuario
            var confirmacion = MessageBox.Show($"¿Confirma el retiro de ₡{monto:N2}?\nMotivo: {motivo}", "Confirmar Retiro", MessageBoxButton.YesNo);
            if (confirmacion != MessageBoxResult.Yes) return;

            try
            {
                long idRetiro = 0;

                using var connection = DBConnection.GetConnection();
                string query = @"
                    INSERT INTO retiro_caja (monto, motivo, fecha, hora) 
                    VALUES (@monto, @motivo, @fecha, @hora);
                    SELECT SCOPE_IDENTITY();";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@monto", monto);
                command.Parameters.AddWithValue("@motivo", motivo);
                command.Parameters.AddWithValue("@fecha", fecha);
                command.Parameters.AddWithValue("@hora", hora);

                object result = command.ExecuteScalar();
                if (result != DBNull.Value && long.TryParse(result.ToString(), out long resultId))
                {
                    idRetiro = resultId;
                }

                // Imprimir recibo
                var sb = new StringBuilder();
                sb.AppendLine("         PULPERÍA CRA");
                sb.AppendLine("      *** RETIRO DE CAJA ***");
                sb.AppendLine("--------------------------------");
                sb.AppendLine($"ID Retiro: {idRetiro}");
                sb.AppendLine($"Usuario: {usuario}");
                sb.AppendLine($"Fecha: {fecha} {hora}");
                sb.AppendLine("--------------------------------");
                sb.AppendLine($"Monto Retirado: ₡{monto:N2}");
                sb.AppendLine($"Motivo: {motivo}");
                sb.AppendLine("--------------------------------");
                sb.AppendLine("  Retiro confirmado y registrado.");
                sb.AppendLine();
                sb.AppendLine("       ¡Gracias por su trabajo!");
                sb.AppendLine();

                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                byte[] textoBytes = Encoding.GetEncoding(850).GetBytes(sb.ToString());
                RawPrinterHelper.SendBytesToPrinter(new PrinterSettings().PrinterName, textoBytes);

                MessageBox.Show("Retiro registrado correctamente.");
                txtMontoRetiro.Clear();
                txtMotivoRetiro.Clear();
                CargarRetiros();
                CalcularDineroEnCaja();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al registrar retiro: " + ex.Message);
            }
        }

        private void CargarRetiros()
        {
            try
            {
                using var connection = DBConnection.GetConnection();
                string query = "SELECT * FROM retiro_caja ORDER BY fecha DESC, hora DESC";

                using var adapter = new SqlDataAdapter(query, connection);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);
                dgRetiros.ItemsSource = dataTable.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar retiros: " + ex.Message);
            }
        }

        private void CalcularDineroEnCaja()
        {
            try
            {
                dineroDisponibleEnCaja = CajaHelper.ObtenerDineroAcumuladoCajaChica();
                txtDineroCaja.Text = $"₡{dineroDisponibleEnCaja:N2}";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al calcular dinero en caja: " + ex.Message);
            }
        }


        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void txtDineroCaja_TextChanged(object sender, TextChangedEventArgs e)
        {
            // No se usa, se puede eliminar si deseas
        }
    }
}
