using PulperiaPOS.DataAccess;
using PulperiaPOS.ApiClients;
using PulperiaPOS.Configuration;
using PulperiaPOS.Models.Api;
using PulperiaPOS.Models.Caja;
using PulperiaPOS.Views;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing.Printing;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PulperiaPOS.Views
{
    public partial class CierreCajaPage : Window
    {
        private const string CajaCodigoCierreApi = "CAJA_PRINCIPAL_TEST";
        private const int MaxCierreApiTextLength = 250;
        private readonly CajaOperationCoordinator cierreCajaCoordinator = new();
        private long? cierreApiTurnoId;
        private string? cierreApiRowVersion;
        private decimal? cierreApiEfectivoEsperado;

        public CierreCajaPage()
        {
            InitializeComponent();
            ConfigureCierreCajaMode();
            _ = CajaApiReadStatusViewHelper.LoadAsync(txtCajaApiStatus, nameof(CierreCajaPage));
        }

        private void ConfigureCierreCajaMode()
        {
            if (!FeatureFlags.UseCajaApiCierreWrite)
            {
                txtCierreCajaModo.Text = "Modo historico SQL para cierre de caja.";
                txtCierreCajaApiEstado.Visibility = Visibility.Collapsed;
                CalcularTotalesDelDia();
                CargarCierresAnteriores();
                return;
            }

            txtCierreCajaModo.Text = $"Modo Caja API para cierre de turno. Caja: {CajaCodigoCierreApi}.";
            txtCierreCajaApiEstado.Visibility = Visibility.Visible;
            txtCierreCajaApiEstado.Text = "Caja API: consultando pre-cierre...";
            txtEfectivo.IsReadOnly = false;
            txtSinpe.IsReadOnly = true;
            txtDatafono.IsReadOnly = true;
            dgCierres.ItemsSource = null;
            _ = RefreshCierreCajaApiStateAsync();
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
            if (FeatureFlags.UseCajaApiCierreWrite)
            {
                _ = CerrarTurnoCajaApiAsync();
                return;
            }

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

        private async Task CerrarTurnoCajaApiAsync()
        {
            if (!TryBuildCierreCajaApiViewModel(out var viewModel))
            {
                return;
            }

            var intentFingerprint = string.Join(
                "|",
                viewModel.IdTurno.ToString(CultureInfo.InvariantCulture),
                viewModel.EfectivoContado.ToString("0.00", CultureInfo.InvariantCulture),
                viewModel.Observacion ?? string.Empty,
                viewModel.RowVersion,
                UserSession.IdUsuario);
            var operation = cierreCajaCoordinator.GetOrCreate("CerrarTurno", intentFingerprint);

            if (!cierreCajaCoordinator.TryBegin(operation))
            {
                MessageBox.Show("El cierre de caja ya esta en proceso.", "Caja API", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SetCierreCajaApiBusy(true);
            txtCierreCajaApiEstado.Text = "Caja API: cerrando turno...";

            try
            {
                var diferenciaEstimada = viewModel.EfectivoContado - viewModel.EfectivoEsperadoEstimado;
                var confirmacion = MessageBox.Show(
                    "Se cerrara el turno de caja mediante Caja API.\n\n" +
                    "Esta operacion es irreversible.\n\n" +
                    $"Efectivo esperado: {viewModel.EfectivoEsperadoEstimado:N2}\n" +
                    $"Efectivo contado: {viewModel.EfectivoContado:N2}\n" +
                    $"Diferencia estimada: {diferenciaEstimada:N2}\n\n" +
                    "Desea confirmar el cierre?",
                    "Confirmar cierre Caja API",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirmacion != MessageBoxResult.Yes)
                {
                    cierreCajaCoordinator.Clear(operation);
                    txtCierreCajaApiEstado.Text = "Caja API: cierre cancelado por el operador.";
                    return;
                }

                var request = new CajaCierreRequest
                {
                    EfectivoContado = viewModel.EfectivoContado,
                    Observacion = viewModel.Observacion,
                    RowVersion = viewModel.RowVersion
                };

                using var client = new CajaApiClient();
                var result = await client.CerrarTurnoAsync(
                    viewModel.IdTurno,
                    request,
                    operation.IdempotencyKey.ToString("D"),
                    CancellationToken.None);

                await HandleCerrarTurnoApiResultAsync(result, operation);
            }
            finally
            {
                SetCierreCajaApiBusy(false);
            }
        }

        private bool TryBuildCierreCajaApiViewModel(out CajaCierreViewModel viewModel)
        {
            viewModel = new CajaCierreViewModel();

            if (!cierreApiTurnoId.HasValue)
            {
                MessageBox.Show("No hay un turno abierto para cerrar.", "Caja API", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!decimal.TryParse(txtEfectivo.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var efectivoContado) &&
                !decimal.TryParse(txtEfectivo.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out efectivoContado))
            {
                MessageBox.Show("El efectivo contado debe ser un valor valido.", "Caja API", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (efectivoContado < 0)
            {
                MessageBox.Show("El efectivo contado debe ser un valor valido.", "Caja API", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!IsValidRowVersion(cierreApiRowVersion))
            {
                MessageBox.Show("La informacion del turno cambio. Actualice la pantalla antes de cerrar.", "Caja API", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            var observacion = txtObservaciones.Text.Trim();
            if (observacion.Length > MaxCierreApiTextLength)
            {
                MessageBox.Show("La observacion es demasiado larga.", "Caja API", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            var diferenciaEstimada = efectivoContado - (cierreApiEfectivoEsperado ?? 0m);
            if (diferenciaEstimada != 0m && string.IsNullOrWhiteSpace(observacion))
            {
                MessageBox.Show("Debe indicar una observacion para justificar la diferencia de cierre.", "Caja API", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            viewModel = new CajaCierreViewModel
            {
                IdTurno = cierreApiTurnoId.Value,
                EfectivoContado = efectivoContado,
                Observacion = string.IsNullOrWhiteSpace(observacion) ? null : observacion,
                RowVersion = cierreApiRowVersion!,
                EfectivoEsperadoEstimado = cierreApiEfectivoEsperado ?? 0m
            };
            return true;
        }

        private async Task HandleCerrarTurnoApiResultAsync(
            ApiRequestResult<CierreCajaApiResponse> result,
            PendingCajaOperation operation)
        {
            if (result.Success && result.Data is not null)
            {
                cierreCajaCoordinator.Clear(operation);
                txtCierreCajaApiEstado.Text = "Caja API: cierre registrado correctamente.";
                MessageBox.Show("El cierre fue registrado correctamente por Caja API.", "Caja API", MessageBoxButton.OK, MessageBoxImage.Information);
                await RefreshCierreCajaApiStateAsync();
                return;
            }

            if (result.ErrorType == ApiErrorType.Timeout)
            {
                cierreCajaCoordinator.MarkResultUncertain(operation);
                txtCierreCajaApiEstado.Text =
                    "Caja API: No fue posible confirmar el resultado del cierre. Revise la conexion y reintente sin cambiar los datos.";
                MessageBox.Show(
                    "No fue posible confirmar el resultado del cierre. Revise la conexion y reintente sin cambiar los datos.",
                    "Caja API",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (result.ErrorType == ApiErrorType.Network)
            {
                cierreCajaCoordinator.MarkReadyForRetry(operation);
                txtCierreCajaApiEstado.Text =
                    "Caja API: No fue posible comunicarse con el servicio de caja. Intente nuevamente.";
                MessageBox.Show(
                    "No fue posible comunicarse con el servicio de caja. Intente nuevamente.",
                    "Caja API",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            cierreCajaCoordinator.Clear(operation);
            ShowCierreCajaApiError(result.ErrorType);
        }

        private async Task RefreshCierreCajaApiStateAsync()
        {
            if (!FeatureFlags.UseCajaApiCierreWrite)
            {
                return;
            }

            try
            {
                using var client = new CajaApiClient();
                var turnoResult = await client.GetTurnoAbiertoAsync(CajaCodigoCierreApi);
                if (!turnoResult.Success)
                {
                    txtCierreCajaApiEstado.Text = $"Caja API: {ToCierreCajaSafeMessage(turnoResult.ErrorType)}";
                    return;
                }

                if (turnoResult.Data is null)
                {
                    cierreApiTurnoId = null;
                    cierreApiRowVersion = null;
                    cierreApiEfectivoEsperado = null;
                    txtCierreCajaApiEstado.Text = "Caja API: No hay un turno abierto para cerrar.";
                    return;
                }

                var preCierreResult = await client.GetPreCierreAsync(turnoResult.Data.IdTurno);
                if (!preCierreResult.Success || preCierreResult.Data is null)
                {
                    txtCierreCajaApiEstado.Text = $"Caja API: {ToCierreCajaSafeMessage(preCierreResult.ErrorType)}";
                    return;
                }

                cierreApiTurnoId = preCierreResult.Data.IdTurno;
                cierreApiRowVersion = preCierreResult.Data.RowVersion;
                cierreApiEfectivoEsperado = preCierreResult.Data.EfectivoEsperado;
                txtEfectivo.Text = preCierreResult.Data.EfectivoEsperado.ToString("N2");
                txtSinpe.Text = "0.00";
                txtDatafono.Text = "0.00";
                txtCierreCajaApiEstado.Text = BuildPreCierreApiStatus(preCierreResult.Data);
            }
            catch
            {
                txtCierreCajaApiEstado.Text = "Caja API: No fue posible comunicarse con el servicio de caja. Intente nuevamente.";
            }
        }

        private static string BuildPreCierreApiStatus(PreCierreCajaApiResponse preCierre)
        {
            var ingresos = 0m;
            var retiros = 0m;
            foreach (var item in preCierre.Resumen)
            {
                if (string.Equals(item.TipoMovimiento, "IngresoCaja", StringComparison.OrdinalIgnoreCase))
                {
                    ingresos += item.Total;
                }
                else if (string.Equals(item.TipoMovimiento, "RetiroCaja", StringComparison.OrdinalIgnoreCase))
                {
                    retiros += item.Total;
                }
            }

            return $"Caja API: turno {preCierre.Estado}. Ingresos {ingresos:N2}. Retiros {retiros:N2}. Efectivo esperado {preCierre.EfectivoEsperado:N2}. Diferencia visual estimada; el resultado oficial lo calcula API.";
        }

        private void ShowCierreCajaApiError(ApiErrorType errorType)
        {
            var message = ToCierreCajaSafeMessage(errorType);
            txtCierreCajaApiEstado.Text = $"Caja API: {message}";
            MessageBox.Show(message, "Caja API", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private static string ToCierreCajaSafeMessage(ApiErrorType errorType)
        {
            return errorType switch
            {
                ApiErrorType.BadRequest => "La solicitud de cierre no es valida.",
                ApiErrorType.Conflict => "La informacion del turno cambio. Actualice la pantalla antes de cerrar.",
                ApiErrorType.Unauthorized or ApiErrorType.SessionExpired => "Su sesion ha vencido. Inicie sesion nuevamente.",
                ApiErrorType.Forbidden => "No tiene permiso para cerrar turnos de caja.",
                ApiErrorType.NotFound => "No hay un turno abierto para cerrar.",
                ApiErrorType.Configuration => "El cierre por API no esta habilitado para este ambiente.",
                ApiErrorType.ServiceError => "No fue posible comunicarse con el servicio de caja. Intente nuevamente.",
                _ => CajaApiErrorMapper.ToSafeUserMessage(errorType)
            };
        }

        private void SetCierreCajaApiBusy(bool isBusy)
        {
            btnGuardarCierre.IsEnabled = !isBusy;
            btnVolver.IsEnabled = !isBusy;
            txtEfectivo.IsEnabled = !isBusy;
            txtObservaciones.IsEnabled = !isBusy;
        }

        private static bool IsValidRowVersion(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            try
            {
                return Convert.FromBase64String(value.Trim()).Length == 8;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private void txtEfectivo_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void txtObservaciones_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
