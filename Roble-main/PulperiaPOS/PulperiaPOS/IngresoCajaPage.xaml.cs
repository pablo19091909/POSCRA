using PulperiaPOS.DataAccess;
using PulperiaPOS.ApiClients;
using PulperiaPOS.Configuration;
using PulperiaPOS.Models.Api;
using PulperiaPOS.Models.Caja;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PulperiaPOS
{
    public partial class IngresoCajaPage : Window
    {
        private const string CajaCodigoAperturaApi = "CAJA_PRINCIPAL_TEST";
        private const string CajaCodigoIngresoApi = "CAJA_PRINCIPAL_TEST";
        private const int MaxIngresoApiTextLength = 250;
        private readonly CajaOperationCoordinator aperturaTurnoCoordinator = new();
        private readonly CajaOperationCoordinator ingresoCajaCoordinator = new();

        public IngresoCajaPage()
        {
            InitializeComponent();
            ConfigureAperturaTurnoApi();
            ConfigureIngresoCajaMode();
            CargarIngresos();
            CalcularDineroEnCaja();
            _ = CajaApiReadStatusViewHelper.LoadAsync(txtCajaApiStatus, nameof(IngresoCajaPage));
        }

        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ConfigureAperturaTurnoApi()
        {
            if (!FeatureFlags.UseCajaApiOpenWrite)
            {
                panelAperturaTurnoApi.Visibility = Visibility.Collapsed;
                return;
            }

            panelAperturaTurnoApi.Visibility = Visibility.Visible;
            txtAperturaTurnoApiEstado.Text = $"Caja API activa para apertura. Caja: {CajaCodigoAperturaApi}.";
            txtFondoInicialTurnoApi.Text = "1000";
        }

        private void ConfigureIngresoCajaMode()
        {
            if (!FeatureFlags.UseCajaApiIngresoWrite)
            {
                txtIngresoCajaModo.Text = "Modo historico SQL para registro de ingresos.";
                txtIngresoCajaApiEstado.Visibility = Visibility.Collapsed;
                panelReferenciaIngresoApi.Visibility = Visibility.Collapsed;
                return;
            }

            txtIngresoCajaModo.Text = $"Modo Caja API para registro de ingresos. Caja: {CajaCodigoIngresoApi}.";
            txtIngresoCajaApiEstado.Visibility = Visibility.Visible;
            txtIngresoCajaApiEstado.Text = "Caja API: validando turno abierto antes de registrar ingresos.";
            panelReferenciaIngresoApi.Visibility = Visibility.Visible;
            _ = RefreshIngresoCajaApiStateAsync();
        }

        private async void AbrirTurnoApi_Click(object sender, RoutedEventArgs e)
        {
            if (!FeatureFlags.UseCajaApiOpenWrite)
            {
                MessageBox.Show("La apertura por API no esta habilitada para este ambiente.", "Caja API", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!decimal.TryParse(txtFondoInicialTurnoApi.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var fondoInicial) &&
                !decimal.TryParse(txtFondoInicialTurnoApi.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out fondoInicial))
            {
                MessageBox.Show("Ingrese un fondo inicial valido.", "Caja API", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (fondoInicial <= 0)
            {
                MessageBox.Show("El fondo inicial debe ser mayor que cero.", "Caja API", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var observacion = txtObservacionTurnoApi.Text.Trim();
            var confirmacion = MessageBox.Show(
                $"Se abrira un turno de caja por API para {CajaCodigoAperturaApi} con fondo inicial ₡{fondoInicial:N2}. ¿Desea continuar?",
                "Confirmar apertura de turno",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmacion != MessageBoxResult.Yes)
            {
                return;
            }

            var intentFingerprint = string.Join(
                "|",
                CajaCodigoAperturaApi,
                fondoInicial.ToString("0.00", CultureInfo.InvariantCulture),
                observacion,
                UserSession.IdUsuario);
            var operation = aperturaTurnoCoordinator.GetOrCreate("AbrirTurno", intentFingerprint);

            if (!aperturaTurnoCoordinator.TryBegin(operation))
            {
                MessageBox.Show("La apertura de turno ya esta en proceso.", "Caja API", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            btnAbrirTurnoApi.IsEnabled = false;
            txtAperturaTurnoApiEstado.Text = "Caja API: abriendo turno...";

            try
            {
                var request = new AbrirCajaTurnoApiRequest
                {
                    CajaCodigo = CajaCodigoAperturaApi,
                    FondoInicial = fondoInicial,
                    Observacion = string.IsNullOrWhiteSpace(observacion) ? null : observacion
                };

                using var client = new CajaApiClient();
                var result = await client.AbrirTurnoAsync(
                    request,
                    operation.IdempotencyKey.ToString("D"),
                    CancellationToken.None);

                await HandleAbrirTurnoApiResultAsync(result, operation);
            }
            finally
            {
                btnAbrirTurnoApi.IsEnabled = true;
            }
        }

        private async Task HandleAbrirTurnoApiResultAsync(
            ApiRequestResult<CajaTurnoApiResponse> result,
            PendingCajaOperation operation)
        {
            if (result.Success && result.Data is not null)
            {
                aperturaTurnoCoordinator.Clear(operation);
                txtAperturaTurnoApiEstado.Text =
                    $"Caja API: turno abierto correctamente. Turno {result.Data.IdTurno}, apertura {result.Data.AperturaUtc:yyyy-MM-dd HH:mm} UTC.";
                MessageBox.Show("El turno de caja fue abierto correctamente.", "Caja API", MessageBoxButton.OK, MessageBoxImage.Information);
                await CajaApiReadStatusViewHelper.LoadAsync(txtCajaApiStatus, nameof(IngresoCajaPage));
                return;
            }

            if (result.ErrorType == ApiErrorType.Timeout)
            {
                aperturaTurnoCoordinator.MarkResultUncertain(operation);
                txtAperturaTurnoApiEstado.Text =
                    "Caja API: no se pudo confirmar el resultado de la apertura. Reintente la misma operacion o verifique el estado de caja.";
                MessageBox.Show(
                    "No se pudo confirmar el resultado de la apertura. No vuelva a intentar con una operacion nueva; verifique el estado de caja o reintente la misma operacion.",
                    "Caja API",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (result.ErrorType == ApiErrorType.Network)
            {
                aperturaTurnoCoordinator.MarkReadyForRetry(operation);
                txtAperturaTurnoApiEstado.Text =
                    "Caja API: No fue posible comunicarse con el servicio de caja. Intente nuevamente.";
                MessageBox.Show(
                    "No fue posible comunicarse con el servicio de caja. Intente nuevamente.",
                    "Caja API",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            aperturaTurnoCoordinator.Clear(operation);

            var message = result.ErrorType switch
            {
                ApiErrorType.Conflict => "Ya existe un turno abierto para esta caja.",
                ApiErrorType.Unauthorized or ApiErrorType.SessionExpired => "Su sesion ha vencido. Inicie sesion nuevamente.",
                ApiErrorType.Forbidden => "No tiene permiso para abrir turnos de caja.",
                ApiErrorType.Configuration => "La apertura por API no esta habilitada para este ambiente.",
                ApiErrorType.ServiceError => "No fue posible comunicarse con el servicio de caja. Intente nuevamente.",
                _ => CajaApiErrorMapper.ToSafeUserMessage(result.ErrorType)
            };

            txtAperturaTurnoApiEstado.Text = $"Caja API: {message}";
            MessageBox.Show(message, "Caja API", MessageBoxButton.OK, MessageBoxImage.Warning);

            if (result.ErrorType == ApiErrorType.Conflict)
            {
                await CajaApiReadStatusViewHelper.LoadAsync(txtCajaApiStatus, nameof(IngresoCajaPage));
            }
        }

        private void RegistrarIngreso_Click(object sender, RoutedEventArgs e)
        {
            if (FeatureFlags.UseCajaApiIngresoWrite)
            {
                _ = RegistrarIngresoCajaApiAsync();
                return;
            }

            RegistrarIngresoHistoricoSql();
        }

        private void RegistrarIngresoHistoricoSql()
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

        private async Task RegistrarIngresoCajaApiAsync()
        {
            if (!TryBuildIngresoCajaApiViewModel(out var viewModel))
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
            var operation = ingresoCajaCoordinator.GetOrCreate("IngresoCaja", intentFingerprint);

            if (!ingresoCajaCoordinator.TryBegin(operation))
            {
                MessageBox.Show("El ingreso de caja ya esta en proceso.", "Caja API", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SetIngresoCajaApiBusy(true);
            txtIngresoCajaApiEstado.Text = "Caja API: registrando ingreso...";

            try
            {
                using var client = new CajaApiClient();
                var turnoResult = await client.GetTurnoAbiertoAsync(CajaCodigoIngresoApi);
                if (!turnoResult.Success)
                {
                    ingresoCajaCoordinator.Clear(operation);
                    ShowIngresoCajaApiError(turnoResult.ErrorType);
                    return;
                }

                if (turnoResult.Data is null)
                {
                    ingresoCajaCoordinator.Clear(operation);
                    txtIngresoCajaApiEstado.Text = "Caja API: No hay un turno abierto para registrar el ingreso.";
                    MessageBox.Show("No hay un turno abierto para registrar el ingreso.", "Caja API", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var request = new CajaIngresoRequest
                {
                    CajaCodigo = viewModel.CajaCodigo,
                    Monto = viewModel.Monto,
                    Motivo = viewModel.Motivo,
                    Referencia = viewModel.Referencia
                };

                var result = await client.RegistrarIngresoAsync(
                    request,
                    operation.IdempotencyKey.ToString("D"),
                    CancellationToken.None);

                await HandleIngresoCajaApiResultAsync(result, operation);
            }
            finally
            {
                SetIngresoCajaApiBusy(false);
            }
        }

        private bool TryBuildIngresoCajaApiViewModel(out CajaIngresoViewModel viewModel)
        {
            viewModel = new CajaIngresoViewModel();

            if (!decimal.TryParse(txtMontoIngreso.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var monto) &&
                !decimal.TryParse(txtMontoIngreso.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out monto))
            {
                MessageBox.Show("Ingrese un monto valido.", "Caja API", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (monto <= 0)
            {
                MessageBox.Show("El monto debe ser mayor que cero.", "Caja API", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            var motivo = txtMotivoIngreso.Text.Trim();
            if (string.IsNullOrWhiteSpace(motivo))
            {
                MessageBox.Show("Ingrese un motivo para el ingreso.", "Caja API", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (motivo.Length > MaxIngresoApiTextLength)
            {
                MessageBox.Show("El motivo es demasiado largo.", "Caja API", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            var referencia = txtReferenciaIngresoApi.Text.Trim();
            if (referencia.Length > MaxIngresoApiTextLength)
            {
                MessageBox.Show("La referencia es demasiado larga.", "Caja API", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            viewModel = new CajaIngresoViewModel
            {
                CajaCodigo = CajaCodigoIngresoApi,
                Monto = monto,
                Motivo = motivo,
                Referencia = string.IsNullOrWhiteSpace(referencia) ? null : referencia
            };
            return true;
        }

        private async Task HandleIngresoCajaApiResultAsync(
            ApiRequestResult<MovimientoCajaApiResponse> result,
            PendingCajaOperation operation)
        {
            if (result.Success && result.Data is not null)
            {
                ingresoCajaCoordinator.Clear(operation);
                txtIngresoCajaApiEstado.Text = "Caja API: ingreso registrado correctamente.";
                MessageBox.Show("El ingreso fue registrado correctamente por Caja API.", "Caja API", MessageBoxButton.OK, MessageBoxImage.Information);
                txtMontoIngreso.Clear();
                txtMotivoIngreso.Clear();
                txtReferenciaIngresoApi.Clear();
                await RefreshIngresoCajaApiStateAsync();
                return;
            }

            if (result.ErrorType == ApiErrorType.Timeout)
            {
                ingresoCajaCoordinator.MarkResultUncertain(operation);
                txtIngresoCajaApiEstado.Text =
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
                ingresoCajaCoordinator.MarkReadyForRetry(operation);
                txtIngresoCajaApiEstado.Text =
                    "Caja API: No fue posible comunicarse con el servicio de caja. Intente nuevamente.";
                MessageBox.Show(
                    "No fue posible comunicarse con el servicio de caja. Intente nuevamente.",
                    "Caja API",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            ingresoCajaCoordinator.Clear(operation);
            ShowIngresoCajaApiError(result.ErrorType);
        }

        private async Task RefreshIngresoCajaApiStateAsync()
        {
            if (!FeatureFlags.UseCajaApiIngresoWrite)
            {
                return;
            }

            try
            {
                using var client = new CajaApiClient();
                var turnoResult = await client.GetTurnoAbiertoAsync(CajaCodigoIngresoApi);
                if (!turnoResult.Success)
                {
                    txtIngresoCajaApiEstado.Text = $"Caja API: {ToIngresoCajaSafeMessage(turnoResult.ErrorType)}";
                    return;
                }

                if (turnoResult.Data is null)
                {
                    txtIngresoCajaApiEstado.Text = "Caja API: No hay un turno abierto para registrar el ingreso.";
                    return;
                }

                var preCierreResult = await client.GetPreCierreAsync(turnoResult.Data.IdTurno);
                if (preCierreResult.Success && preCierreResult.Data is not null)
                {
                    txtIngresoCajaApiEstado.Text =
                        $"Caja API: turno {turnoResult.Data.Estado}. Efectivo esperado: {preCierreResult.Data.EfectivoEsperado:N2}.";
                    return;
                }

                txtIngresoCajaApiEstado.Text = $"Caja API: turno {turnoResult.Data.Estado}.";
            }
            catch
            {
                txtIngresoCajaApiEstado.Text = "Caja API: No fue posible consultar el estado de caja.";
            }
        }

        private void ShowIngresoCajaApiError(ApiErrorType errorType)
        {
            var message = ToIngresoCajaSafeMessage(errorType);
            txtIngresoCajaApiEstado.Text = $"Caja API: {message}";
            MessageBox.Show(message, "Caja API", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private static string ToIngresoCajaSafeMessage(ApiErrorType errorType)
        {
            return errorType switch
            {
                ApiErrorType.Conflict => "No hay un turno abierto para registrar el ingreso.",
                ApiErrorType.Unauthorized or ApiErrorType.SessionExpired => "Su sesion ha vencido. Inicie sesion nuevamente.",
                ApiErrorType.Forbidden => "No tiene permiso para registrar ingresos de caja.",
                ApiErrorType.NotFound => "No hay un turno abierto para registrar el ingreso.",
                ApiErrorType.Configuration => "El registro de ingresos por API no esta habilitado para este ambiente.",
                ApiErrorType.ServiceError => "No fue posible comunicarse con el servicio de caja. Intente nuevamente.",
                _ => CajaApiErrorMapper.ToSafeUserMessage(errorType)
            };
        }

        private void SetIngresoCajaApiBusy(bool isBusy)
        {
            btnRegistrarIngreso.IsEnabled = !isBusy;
            txtMontoIngreso.IsEnabled = !isBusy;
            txtMotivoIngreso.IsEnabled = !isBusy;
            txtReferenciaIngresoApi.IsEnabled = !isBusy;
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
