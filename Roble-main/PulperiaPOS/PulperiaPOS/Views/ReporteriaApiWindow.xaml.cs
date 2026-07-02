using PulperiaPOS.ApiClients;
using PulperiaPOS.Configuration;
using PulperiaPOS.Models.Api;
using PulperiaPOS.Models.Reportes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PulperiaPOS.Views
{
    public partial class ReporteriaApiWindow : Window
    {
        private const string SafeReportesError = "No fue posible cargar la reporteria desde el servicio API. Intente nuevamente.";
        private readonly List<ReporteVentaDetalleResponse> ventas = [];
        private readonly List<ReporteReversaResponse> reversas = [];
        private readonly List<ReporteTurnoCajaResponse> turnos = [];
        private readonly List<ReporteMovimientoCajaResponse> movimientos = [];
        private readonly List<ReporteInconsistenciaResponse> alertas = [];
        private bool loading;

        public ReporteriaApiWindow()
        {
            InitializeComponent();
            Loaded += ReporteriaApiWindow_Loaded;
        }

        private async void ReporteriaApiWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!FeatureFlags.UseReportesApiRead)
            {
                txtModo.Text = "Reporteria API deshabilitada";
                txtEstado.Text = "UseReportesApiRead=false. Los reportes historicos existentes se conservan sin cambios.";
                ClearReportData();
                return;
            }

            txtModo.Text = "Modo Reporteria API";
            await CargarReporteriaAsync();
        }

        private async void BtnRefrescar_Click(object sender, RoutedEventArgs e)
        {
            if (!FeatureFlags.UseReportesApiRead)
            {
                txtEstado.Text = "UseReportesApiRead=false. No se llamo a la API de reporteria.";
                ClearReportData();
                return;
            }

            await CargarReporteriaAsync();
        }

        private async Task CargarReporteriaAsync()
        {
            if (loading)
            {
                return;
            }

            loading = true;
            txtEstado.Text = "Cargando reporteria API...";
            SetControlsEnabled(false);

            try
            {
                var desdeUtc = dpDesde.SelectedDate?.Date.ToUniversalTime();
                var hastaUtc = dpHasta.SelectedDate?.Date.AddDays(1).ToUniversalTime();

                using var client = new ReportesApiClient();
                var resumenVentas = await client.GetVentasResumenAsync(desdeUtc, hastaUtc);
                var detalleVentas = await client.GetVentasDetalleAsync(desdeUtc, hastaUtc, limit: 200);
                var reversasResult = await client.GetReversasAsync(desdeUtc, hastaUtc, limit: 200);
                var resumenCaja = await client.GetCajaResumenAsync(desdeUtc, hastaUtc);
                var turnosResult = await client.GetTurnosAsync(desdeUtc, hastaUtc, limit: 200);
                var movimientosResult = await client.GetMovimientosAsync(desdeUtc, hastaUtc, limit: 200);
                var alertasResult = await client.GetInconsistenciasAsync();

                if (!AllSucceeded(resumenVentas, detalleVentas, reversasResult, resumenCaja, turnosResult, movimientosResult, alertasResult))
                {
                    ClearReportData();
                    txtEstado.Text = SafeReportesError;
                    return;
                }

                MostrarResumen(resumenVentas.Data, resumenCaja.Data);
                ventas.Clear();
                ventas.AddRange(detalleVentas.Data ?? []);
                reversas.Clear();
                reversas.AddRange(reversasResult.Data ?? []);
                turnos.Clear();
                turnos.AddRange(turnosResult.Data ?? []);
                movimientos.Clear();
                movimientos.AddRange(movimientosResult.Data ?? []);
                alertas.Clear();
                alertas.AddRange((alertasResult.Data ?? []).Where(a => a.Cantidad > 0));

                AplicarFiltros();
                txtEstado.Text = "Reporteria API cargada. Fuente visible: API e historico etiquetado segun contrato.";
            }
            finally
            {
                SetControlsEnabled(true);
                loading = false;
            }
        }

        private static bool AllSucceeded(params ApiRequestResult<object>[] results)
        {
            return results.All(result => result.Success);
        }

        private static bool AllSucceeded(
            ApiRequestResult<ReporteVentasResumenResponse> ventasResumen,
            ApiRequestResult<IReadOnlyCollection<ReporteVentaDetalleResponse>> ventasDetalle,
            ApiRequestResult<IReadOnlyCollection<ReporteReversaResponse>> reversasResult,
            ApiRequestResult<ReporteCajaResumenResponse> cajaResumen,
            ApiRequestResult<IReadOnlyCollection<ReporteTurnoCajaResponse>> turnosResult,
            ApiRequestResult<IReadOnlyCollection<ReporteMovimientoCajaResponse>> movimientosResult,
            ApiRequestResult<IReadOnlyCollection<ReporteInconsistenciaResponse>> alertasResult)
        {
            return ventasResumen.Success &&
                ventasDetalle.Success &&
                reversasResult.Success &&
                cajaResumen.Success &&
                turnosResult.Success &&
                movimientosResult.Success &&
                alertasResult.Success;
        }

        private void MostrarResumen(ReporteVentasResumenResponse? ventasResumen, ReporteCajaResumenResponse? cajaResumen)
        {
            txtVentasBrutas.Text = Money(ventasResumen?.VentasBrutas ?? 0);
            txtMontoReversado.Text = Money(ventasResumen?.MontoReversado ?? 0);
            txtVentasNetas.Text = Money(ventasResumen?.VentasNetas ?? 0);
            txtCantidadReversas.Text = (ventasResumen?.CantidadReversas ?? 0).ToString(CultureInfo.InvariantCulture);
            txtEfectivoBruto.Text = Money(ventasResumen?.EfectivoVentasBruto ?? 0);
            txtReversasEfectivo.Text = Money(ventasResumen?.ReversasEfectivo ?? 0);
            txtEfectivoNeto.Text = Money(ventasResumen?.EfectivoVentasNeto ?? 0);

            txtTurnosAbiertos.Text = (cajaResumen?.TurnosAbiertos ?? 0).ToString(CultureInfo.InvariantCulture);
            txtTurnosCerrados.Text = (cajaResumen?.TurnosCerrados ?? 0).ToString(CultureInfo.InvariantCulture);
            txtCierreDiferencia.Text = Money(cajaResumen?.CierreDiferencia ?? 0);
            txtEfectivoEsperado.Text = Money(cajaResumen?.EfectivoEsperadoCalculado ?? 0);
            txtDiferencia.Text = Money(cajaResumen?.DiferenciaCerrada ?? 0);
        }

        private void AplicarFiltros()
        {
            var estado = SelectedComboText(cmbEstado);
            var origen = SelectedComboText(cmbOrigen);
            var turno = txtTurnoFiltro.Text.Trim();
            var soloEfectivo = chkSoloEfectivo.IsChecked == true;

            IEnumerable<ReporteVentaDetalleResponse> ventasFiltradas = ventas;
            if (estado == "Activas")
            {
                ventasFiltradas = ventasFiltradas.Where(v => !v.Reversada);
            }
            else if (estado == "Reversadas")
            {
                ventasFiltradas = ventasFiltradas.Where(v => v.Reversada);
            }

            if (origen is "Venta API" or "Historico SQL")
            {
                ventasFiltradas = ventasFiltradas.Where(v => string.Equals(v.Origen, origen, StringComparison.OrdinalIgnoreCase));
            }

            if (soloEfectivo)
            {
                ventasFiltradas = ventasFiltradas.Where(v => string.Equals(v.MetodoPago, "Efectivo", StringComparison.OrdinalIgnoreCase));
            }

            IEnumerable<ReporteTurnoCajaResponse> turnosFiltrados = turnos;
            IEnumerable<ReporteMovimientoCajaResponse> movimientosFiltrados = movimientos;

            if (origen == "Caja API")
            {
                turnosFiltrados = turnosFiltrados.Where(t => string.Equals(t.Fuente, "Caja API", StringComparison.OrdinalIgnoreCase));
                movimientosFiltrados = movimientosFiltrados.Where(m => !string.IsNullOrWhiteSpace(m.Origen));
            }

            if (!string.IsNullOrWhiteSpace(turno))
            {
                turnosFiltrados = turnosFiltrados.Where(t => t.CajaCodigo.Contains(turno, StringComparison.OrdinalIgnoreCase) || t.Estado.Contains(turno, StringComparison.OrdinalIgnoreCase));
                movimientosFiltrados = movimientosFiltrados.Where(m => m.TipoMovimiento.Contains(turno, StringComparison.OrdinalIgnoreCase) || m.Origen.Contains(turno, StringComparison.OrdinalIgnoreCase));
            }

            dgVentas.ItemsSource = ventasFiltradas.ToArray();
            dgReversas.ItemsSource = reversas.ToArray();
            dgTurnos.ItemsSource = turnosFiltrados.ToArray();
            dgMovimientos.ItemsSource = movimientosFiltrados.ToArray();
            dgAlertas.ItemsSource = alertas.ToArray();

            if (!ventas.Any() && !reversas.Any() && !turnos.Any() && !movimientos.Any())
            {
                txtEstado.Text = "No hay datos para el rango seleccionado.";
            }
        }

        private void ClearReportData()
        {
            MostrarResumen(null, null);
            ventas.Clear();
            reversas.Clear();
            turnos.Clear();
            movimientos.Clear();
            alertas.Clear();
            dgVentas.ItemsSource = Array.Empty<ReporteVentaDetalleResponse>();
            dgReversas.ItemsSource = Array.Empty<ReporteReversaResponse>();
            dgTurnos.ItemsSource = Array.Empty<ReporteTurnoCajaResponse>();
            dgMovimientos.ItemsSource = Array.Empty<ReporteMovimientoCajaResponse>();
            dgAlertas.ItemsSource = Array.Empty<ReporteInconsistenciaResponse>();
        }

        private void SetControlsEnabled(bool enabled)
        {
            dpDesde.IsEnabled = enabled;
            dpHasta.IsEnabled = enabled;
            cmbEstado.IsEnabled = enabled;
            cmbOrigen.IsEnabled = enabled;
            txtTurnoFiltro.IsEnabled = enabled;
            chkSoloEfectivo.IsEnabled = enabled;
        }

        private void Filtros_Changed(object sender, RoutedEventArgs e)
        {
            if (!loading && IsLoaded)
            {
                AplicarFiltros();
            }
        }

        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static string SelectedComboText(ComboBox comboBox)
        {
            return (comboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;
        }

        private static string Money(decimal value)
        {
            return value.ToString("C2", CultureInfo.GetCultureInfo("es-CR"));
        }
    }
}
