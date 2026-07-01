using PulperiaPOS.DataAccess;
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
using PulperiaPOS.ApiClients;
using PulperiaPOS.Configuration;
using PulperiaPOS.Models.Api;
using PulperiaPOS.Models.Caja;

namespace PulperiaPOS.Views
{
    public partial class RetirosCajaPage : Window
    {
        private const string CajaCodigoRetiroApi = "CAJA_PRINCIPAL_TEST";
        private const int MaxRetiroApiTextLength = 250;
        private double dineroDisponibleEnCaja = 0;
        private readonly CajaOperationCoordinator retiroCajaCoordinator = new();

        public RetirosCajaPage()
        {
            InitializeComponent();
            ConfigureRetiroCajaMode();
            CargarRetiros();
            CalcularDineroEnCaja();
            _ = CajaApiReadStatusViewHelper.LoadAsync(txtCajaApiStatus, nameof(RetirosCajaPage));
        }

        private void ConfigureRetiroCajaMode()
        {
            if (!FeatureFlags.UseCajaApiRetiroWrite)
            {
                txtRetiroCajaModo.Text = "Modo historico SQL para registro de retiros.";
                txtRetiroCajaApiEstado.Visibility = Visibility.Collapsed;
                panelReferenciaRetiroApi.Visibility = Visibility.Collapsed;
                return;
            }

            txtRetiroCajaModo.Text = $"Modo Caja API para registro de retiros. Caja: {CajaCodigoRetiroApi}.";
            txtRetiroCajaApiEstado.Visibility = Visibility.Visible;
            txtRetiroCajaApiEstado.Text = "Caja API: validando turno abierto antes de registrar retiros.";
            panelReferenciaRetiroApi.Visibility = Visibility.Visible;
            _ = RefreshRetiroCajaApiStateAsync();
        }

        private async void RegistrarRetiro_Click(object sender, RoutedEventArgs e)
        {
            if (FeatureFlags.UseCajaApiRetiroWrite)
            {
                await RegistrarRetiroCajaApiAsync();
                return;
            }

            RegistrarRetiroHistoricoSql();
        }

        private void RegistrarRetiroHistoricoSql()
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

        private async Task RegistrarRetiroCajaApiAsync()
        {
            if (!TryBuildRetiroCajaApiViewModel(out var viewModel))
            {
                return;
            }

            var intentFingerprint = string.Join(
                "|",
                viewModel.CajaCodigo,
                viewModel.Monto.ToString("0.00", CultureInfo.InvariantCulture),
                viewModel.Motivo,
                viewModel.Referencia ?? string.Empty,
                UserSession.IdUsuario);
            var operation = retiroCajaCoordinator.GetOrCreate("RetiroCaja", intentFingerprint);

            if (!retiroCajaCoordinator.TryBegin(operation))
            {
                MessageBox.Show("El retiro de caja ya esta en proceso.", "Caja API", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SetRetiroCajaApiBusy(true);
            txtRetiroCajaApiEstado.Text = "Caja API: registrando retiro...";

            try
            {
                using var client = new CajaApiClient();
                var turnoResult = await client.GetTurnoAbiertoAsync(CajaCodigoRetiroApi);
                if (!turnoResult.Success)
                {
                    retiroCajaCoordinator.Clear(operation);
                    ShowRetiroCajaApiError(turnoResult.ErrorType);
                    return;
                }

                if (turnoResult.Data is null)
                {
                    retiroCajaCoordinator.Clear(operation);
                    txtRetiroCajaApiEstado.Text = "Caja API: No hay un turno abierto para registrar el retiro.";
                    MessageBox.Show("No hay un turno abierto para registrar el retiro.", "Caja API", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var preCierreResult = await client.GetPreCierreAsync(turnoResult.Data.IdTurno);
                if (!preCierreResult.Success)
                {
                    retiroCajaCoordinator.Clear(operation);
                    ShowRetiroCajaApiError(preCierreResult.ErrorType);
                    return;
                }

                if (preCierreResult.Data is not null)
                {
                    txtRetiroCajaApiEstado.Text =
                        $"Caja API: efectivo disponible informado por API: {preCierreResult.Data.EfectivoEsperado:N2}. Registrando retiro...";
                }

                var request = new CajaRetiroRequest
                {
                    CajaCodigo = viewModel.CajaCodigo,
                    Monto = viewModel.Monto,
                    Motivo = viewModel.Motivo,
                    Referencia = viewModel.Referencia
                };

                var result = await client.RegistrarRetiroAsync(
                    request,
                    operation.IdempotencyKey.ToString("D"),
                    CancellationToken.None);

                await HandleRetiroCajaApiResultAsync(result, operation);
            }
            finally
            {
                SetRetiroCajaApiBusy(false);
            }
        }

        private bool TryBuildRetiroCajaApiViewModel(out CajaRetiroViewModel viewModel)
        {
            viewModel = new CajaRetiroViewModel();

            if (!decimal.TryParse(txtMontoRetiro.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var monto) &&
                !decimal.TryParse(txtMontoRetiro.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out monto))
            {
                MessageBox.Show("Ingrese un monto valido.", "Caja API", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (monto <= 0)
            {
                MessageBox.Show("El monto debe ser mayor que cero.", "Caja API", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            var motivo = txtMotivoRetiro.Text.Trim();
            if (string.IsNullOrWhiteSpace(motivo))
            {
                MessageBox.Show("Ingrese un motivo para el retiro.", "Caja API", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (motivo.Length > MaxRetiroApiTextLength)
            {
                MessageBox.Show("El motivo es demasiado largo.", "Caja API", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            var referencia = txtReferenciaRetiroApi.Text.Trim();
            if (referencia.Length > MaxRetiroApiTextLength)
            {
                MessageBox.Show("La referencia es demasiado larga.", "Caja API", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            viewModel = new CajaRetiroViewModel
            {
                CajaCodigo = CajaCodigoRetiroApi,
                Monto = monto,
                Motivo = motivo,
                Referencia = string.IsNullOrWhiteSpace(referencia) ? null : referencia
            };
            return true;
        }

        private async Task HandleRetiroCajaApiResultAsync(
            ApiRequestResult<MovimientoCajaApiResponse> result,
            PendingCajaOperation operation)
        {
            if (result.Success && result.Data is not null)
            {
                retiroCajaCoordinator.Clear(operation);
                txtRetiroCajaApiEstado.Text = "Caja API: retiro registrado correctamente.";
                MessageBox.Show("El retiro fue registrado correctamente por Caja API.", "Caja API", MessageBoxButton.OK, MessageBoxImage.Information);
                txtMontoRetiro.Clear();
                txtMotivoRetiro.Clear();
                txtReferenciaRetiroApi.Clear();
                await RefreshRetiroCajaApiStateAsync();
                return;
            }

            if (result.ErrorType == ApiErrorType.Timeout)
            {
                retiroCajaCoordinator.MarkResultUncertain(operation);
                txtRetiroCajaApiEstado.Text =
                    "Caja API: No fue posible confirmar el resultado de la operacion. Revise la conexion y reintente sin cambiar los datos.";
                MessageBox.Show(
                    "No fue posible confirmar el resultado de la operacion. Revise la conexion y reintente sin cambiar los datos.",
                    "Caja API",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (result.ErrorType == ApiErrorType.Network)
            {
                retiroCajaCoordinator.MarkReadyForRetry(operation);
                txtRetiroCajaApiEstado.Text =
                    "Caja API: No fue posible comunicarse con el servicio de caja. Intente nuevamente.";
                MessageBox.Show(
                    "No fue posible comunicarse con el servicio de caja. Intente nuevamente.",
                    "Caja API",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            retiroCajaCoordinator.Clear(operation);
            ShowRetiroCajaApiError(result.ErrorType);
        }

        private async Task RefreshRetiroCajaApiStateAsync()
        {
            if (!FeatureFlags.UseCajaApiRetiroWrite)
            {
                return;
            }

            try
            {
                using var client = new CajaApiClient();
                var turnoResult = await client.GetTurnoAbiertoAsync(CajaCodigoRetiroApi);
                if (!turnoResult.Success)
                {
                    txtRetiroCajaApiEstado.Text = $"Caja API: {ToRetiroCajaSafeMessage(turnoResult.ErrorType)}";
                    return;
                }

                if (turnoResult.Data is null)
                {
                    txtRetiroCajaApiEstado.Text = "Caja API: No hay un turno abierto para registrar el retiro.";
                    return;
                }

                var preCierreResult = await client.GetPreCierreAsync(turnoResult.Data.IdTurno);
                if (preCierreResult.Success && preCierreResult.Data is not null)
                {
                    txtRetiroCajaApiEstado.Text =
                        $"Caja API: turno {turnoResult.Data.Estado}. Efectivo disponible informado por API: {preCierreResult.Data.EfectivoEsperado:N2}.";
                    return;
                }

                txtRetiroCajaApiEstado.Text = $"Caja API: turno {turnoResult.Data.Estado}.";
            }
            catch
            {
                txtRetiroCajaApiEstado.Text = "Caja API: No fue posible consultar el estado de caja.";
            }
        }

        private void ShowRetiroCajaApiError(ApiErrorType errorType)
        {
            var message = ToRetiroCajaSafeMessage(errorType);
            txtRetiroCajaApiEstado.Text = $"Caja API: {message}";
            MessageBox.Show(message, "Caja API", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private static string ToRetiroCajaSafeMessage(ApiErrorType errorType)
        {
            return errorType switch
            {
                ApiErrorType.Conflict => "El monto solicitado supera el efectivo disponible de la caja o el turno no permite retiros.",
                ApiErrorType.Unauthorized or ApiErrorType.SessionExpired => "Su sesion ha vencido. Inicie sesion nuevamente.",
                ApiErrorType.Forbidden => "No tiene permiso para registrar retiros de caja.",
                ApiErrorType.NotFound => "No hay un turno abierto para registrar el retiro.",
                ApiErrorType.Configuration => "El registro de retiros por API no esta habilitado para este ambiente.",
                ApiErrorType.ServiceError => "No fue posible comunicarse con el servicio de caja. Intente nuevamente.",
                _ => CajaApiErrorMapper.ToSafeUserMessage(errorType)
            };
        }

        private void SetRetiroCajaApiBusy(bool isBusy)
        {
            btnRegistrarRetiro.IsEnabled = !isBusy;
            txtMontoRetiro.IsEnabled = !isBusy;
            txtMotivoRetiro.IsEnabled = !isBusy;
            txtReferenciaRetiroApi.IsEnabled = !isBusy;
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
