using PulperiaPOS.DataAccess;
using PulperiaPOS.Views;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing.Printing;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace PulperiaPOS.Views
{
    public partial class CierreCajaPage : Window
    {
        public CierreCajaPage()
        {
            InitializeComponent();
            CalcularTotalesDelDia();
            CargarCierresAnteriores();
            _ = CajaApiReadStatusViewHelper.LoadAsync(txtCajaApiStatus, nameof(CierreCajaPage));
        }

        private void CalcularTotalesDelDia()
        {
            decimal sinpeVentas = 0, datafono = 0;

            try
            {
                var totales = CajaHelper.ObtenerTotalesCaja();
                txtEfectivo.Text = totales.TotalDisponible.ToString("N2");

                string fechaHoy = DateTime.Now.ToString("yyyy-MM-dd");

                using (var connection = DBConnection.GetConnection())
                {
                    string query = @"SELECT metodo_pago, SUM(total) AS total 
                             FROM ventas 
                             WHERE CAST(fecha AS DATE) = @fecha 
                             GROUP BY metodo_pago";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@fecha", fechaHoy);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string metodo = reader["metodo_pago"].ToString().ToLower();
                                decimal monto = reader["total"] != DBNull.Value ? Convert.ToDecimal(reader["total"]) : 0;

                                switch (metodo)
                                {
                                    case "sinpe":
                                        sinpeVentas += monto;
                                        break;
                                    case "datafono":
                                    case "voucher":
                                    case "tarjeta":
                                    case "datáfono":
                                        datafono += monto;
                                        break;
                                }
                            }
                        }
                    }
                }

                decimal totalSinpe = sinpeVentas + Convert.ToDecimal(totales.SinpeClientes);
                txtSinpe.Text = totalSinpe.ToString("N2");
                txtDatafono.Text = datafono.ToString("N2");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al calcular totales: " + ex.Message);
            }
        }


        private void GuardarCierre_Click(object sender, RoutedEventArgs e)
        {
            RawPrinterHelper.AbrirCajaDesdePOS58();

            var confirmacion = MessageBox.Show(
                "La caja ha sido abierta. Cuente el dinero.\n\n¿Desea continuar con el cierre de caja?",
                "Confirmar cierre",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmacion != MessageBoxResult.Yes)
            {
                MessageBox.Show("Operación cancelada. Verifique el efectivo y vuelva a intentar.");
                return;
            }

            string fecha = DateTime.Now.ToString("yyyy-MM-dd");
            string hora = DateTime.Now.ToString("HH:mm:ss");
            string observaciones = txtObservaciones.Text;
            long idCierre = 0;

            try
            {
                using (var connection = DBConnection.GetConnection())
                {
                    string query = @"INSERT INTO cierre_caja 
                                     (fecha, hora, total_efectivo, total_sinpe, total_datafono, observaciones) 
                                     VALUES (@fecha, @hora, @efectivo, @sinpe, @datafono, @observaciones);
                                     SELECT SCOPE_IDENTITY();";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@fecha", fecha);
                        command.Parameters.AddWithValue("@hora", hora);
                        command.Parameters.AddWithValue("@efectivo", Convert.ToDecimal(txtEfectivo.Text));
                        command.Parameters.AddWithValue("@sinpe", Convert.ToDecimal(txtSinpe.Text));
                        command.Parameters.AddWithValue("@datafono", Convert.ToDecimal(txtDatafono.Text));
                        command.Parameters.AddWithValue("@observaciones", observaciones);

                        object result = command.ExecuteScalar();
                        idCierre = Convert.ToInt64(result);
                    }

                    var totales = CajaHelper.ObtenerTotalesCaja();

                    RawPrinterHelper.ImprimirCierreDeCaja(
                        new PrinterSettings().PrinterName,
                        idCierre,
                        fecha + " " + hora,
                        txtEfectivo.Text,
                        txtSinpe.Text,
                        txtDatafono.Text,
                        observaciones,
                        UserSession.NombreUsuario,
                        totales
                    );
                }

                MessageBox.Show("Cierre de caja guardado correctamente.");
                CargarCierresAnteriores();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar el cierre: " + ex.Message);
            }
        }

        public static void ImprimirCierreDeCaja(string printerName, long idCierre, string fechaHora,
                                                 string efectivo, string sinpe, string datafono,
                                                 string observaciones, string usuario,
                                                 CajaHelper.TotalesCaja totales)
        {
            var sb = new StringBuilder();

            sb.AppendLine("       PULPERÍA CRA");
            sb.AppendLine("      Cierre de Caja del Día");
            sb.AppendLine("-------------------------------");
            sb.AppendLine("ID Cierre: " + idCierre);
            sb.AppendLine("Fecha: " + fechaHora);
            sb.AppendLine("Cajero: " + usuario);
            sb.AppendLine();
            sb.AppendLine(">>> RESUMEN DE EFECTIVO <<<");
            sb.AppendLine($"Ventas en Efectivo:   ₡{totales.Ventas:N2}");
            sb.AppendLine($"Ingresos Manuales:    ₡{totales.Ingresos:N2}");
            sb.AppendLine($"Retiros del Día:     -₡{totales.Retiros:N2}");
            sb.AppendLine($"Cierres Anteriores:  -₡{totales.Cierres:N2}");
            sb.AppendLine("-------------------------------");
            sb.AppendLine($"Total en Caja:        ₡{totales.TotalDisponible:N2}");
            sb.AppendLine();
            sb.AppendLine(">>> OTROS MÉTODOS <<<");
            sb.AppendLine($"SINPE:                ₡{sinpe}");
            sb.AppendLine($"Datáfono:             ₡{datafono}");
            sb.AppendLine();
            sb.AppendLine("Observaciones:");
            sb.AppendLine(observaciones);
            sb.AppendLine();
            sb.AppendLine("-------------------------------");
            sb.AppendLine("Gracias por su esfuerzo.");
            sb.AppendLine();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var encoding = Encoding.GetEncoding(850);
            var bytes = encoding.GetBytes(sb.ToString());

            RawPrinterHelper.SendBytesToPrinter(printerName, bytes);
        }

        private void CargarCierresAnteriores()
        {
            try
            {
                using (var connection = DBConnection.GetConnection())
                {
                    string query = @"SELECT idCierre, fecha, hora, total_efectivo, total_sinpe, total_datafono, observaciones 
                                     FROM cierre_caja 
                                     ORDER BY fecha DESC, hora DESC";

                    using (var adapter = new SqlDataAdapter(query, connection))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);

                        if (!dataTable.Columns.Contains("fecha_hora"))
                            dataTable.Columns.Add("fecha_hora", typeof(string));

                        foreach (DataRow row in dataTable.Rows)
                        {
                            row["fecha_hora"] = $"{row["fecha"]} {row["hora"]}";
                        }

                        dgCierres.ItemsSource = dataTable.DefaultView;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar cierres: " + ex.Message);
            }
        }

        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void txtEfectivo_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void txtObservaciones_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
