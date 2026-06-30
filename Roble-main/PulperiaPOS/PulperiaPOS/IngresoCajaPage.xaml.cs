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
        private readonly CajaOperationCoordinator aperturaTurnoCoordinator = new();

        public IngresoCajaPage()
        {
            InitializeComponent();
            ConfigureAperturaTurnoApi();
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
