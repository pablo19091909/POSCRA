using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using PulperiaPOS.ApiClients;
using PulperiaPOS.Configuration;
using PulperiaPOS.Models.Caja;

namespace PulperiaPOS
{
    public static class CajaApiReadStatusViewHelper
    {
        private const string CajaCodigoLectura = "CAJA_PRINCIPAL_TEST";

        public static async Task LoadAsync(TextBlock statusTextBlock, CancellationToken cancellationToken = default)
            => await LoadAsync(statusTextBlock, "PantallaCaja", cancellationToken);

        public static async Task LoadAsync(TextBlock statusTextBlock, string screenName, CancellationToken cancellationToken = default)
        {
            if (!FeatureFlags.UseCajaApiRead)
            {
                statusTextBlock.Visibility = Visibility.Collapsed;
                statusTextBlock.Text = string.Empty;
                WriteSafeLog(screenName, flagEnabled: false, queryStarted: false, httpResult: "not_run", finalVisibility: "oculto", message: string.Empty);
                return;
            }

            statusTextBlock.Visibility = Visibility.Visible;
            statusTextBlock.Text = "Caja API: consultando estado del turno...";
            WriteSafeLog(screenName, flagEnabled: true, queryStarted: true, httpResult: "iniciada", finalVisibility: "visible", message: statusTextBlock.Text);

            try
            {
                using var client = new CajaApiClient();
                var turnoResult = await client.GetTurnoAbiertoAsync(CajaCodigoLectura, cancellationToken);
                if (!turnoResult.Success)
                {
                    statusTextBlock.Text = $"Caja API: {CajaApiErrorMapper.ToSafeUserMessage(turnoResult.ErrorType)}";
                    WriteSafeLog(screenName, flagEnabled: true, queryStarted: true, httpResult: turnoResult.ErrorType.ToString(), finalVisibility: "visible", message: statusTextBlock.Text);
                    return;
                }

                if (turnoResult.Data is null)
                {
                    statusTextBlock.Text = $"Caja API: No existe un turno de caja abierto para {CajaCodigoLectura}.";
                    WriteSafeLog(screenName, flagEnabled: true, queryStarted: true, httpResult: "204", finalVisibility: "visible", message: statusTextBlock.Text);
                    return;
                }

                var turno = turnoResult.Data;
                var builder = new StringBuilder();
                builder.Append($"Caja API: turno {turno.Estado}. Fondo inicial: {turno.FondoInicial:N2}.");

                var movimientosResult = await client.GetMovimientosAsync(turno.IdTurno, cancellationToken);
                if (movimientosResult.Success)
                {
                    var movimientos = movimientosResult.Data ?? Array.Empty<MovimientoCajaApiResponse>();
                    builder.Append($" Movimientos: {movimientos.Count}.");
                }
                else
                {
                    builder.Append($" Movimientos: {CajaApiErrorMapper.ToSafeUserMessage(movimientosResult.ErrorType)}");
                }

                var preCierreResult = await client.GetPreCierreAsync(turno.IdTurno, cancellationToken);
                if (preCierreResult.Success && preCierreResult.Data is not null)
                {
                    builder.Append($" Efectivo esperado: {preCierreResult.Data.EfectivoEsperado:N2}.");
                    var resumen = preCierreResult.Data.Resumen;
                    if (resumen.Count > 0)
                    {
                        builder.Append(" Resumen: ");
                        builder.Append(string.Join("; ", resumen.Select(item => $"{item.TipoMovimiento}: {item.Total:N2}")));
                        builder.Append('.');
                    }
                }
                else if (!preCierreResult.Success)
                {
                    builder.Append($" Pre-cierre: {CajaApiErrorMapper.ToSafeUserMessage(preCierreResult.ErrorType)}");
                }

                statusTextBlock.Text = builder.ToString();
                WriteSafeLog(screenName, flagEnabled: true, queryStarted: true, httpResult: "200", finalVisibility: "visible", message: statusTextBlock.Text);
            }
            catch (OperationCanceledException)
            {
                statusTextBlock.Text = "Caja API: La consulta fue cancelada.";
                WriteSafeLog(screenName, flagEnabled: true, queryStarted: true, httpResult: "cancelada", finalVisibility: "visible", message: statusTextBlock.Text);
            }
            catch
            {
                statusTextBlock.Text = "Caja API: No fue posible consultar el estado de caja.";
                WriteSafeLog(screenName, flagEnabled: true, queryStarted: true, httpResult: "error_seguro", finalVisibility: "visible", message: statusTextBlock.Text);
            }
        }

        private static void WriteSafeLog(string screenName, bool flagEnabled, bool queryStarted, string httpResult, string finalVisibility, string message)
        {
            try
            {
                var logDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "PulperiaPOS",
                    "Logs");
                Directory.CreateDirectory(logDirectory);

                var apiBaseUrlState = string.IsNullOrWhiteSpace(AppConfiguration.Current["Api:BaseUrl"])
                    ? "no_configurada"
                    : "configurada";
                var tokenAvailable =
                    UserSession.IsApiAuthenticated &&
                    !string.IsNullOrWhiteSpace(UserSession.AccessToken) &&
                    UserSession.TokenExpiresAtUtc.HasValue &&
                    UserSession.TokenExpiresAtUtc.Value > DateTimeOffset.UtcNow;

                var line = string.Join(
                    " | ",
                    DateTimeOffset.UtcNow.ToString("O"),
                    "[CajaApiRead]",
                    $"Flag efectivo: {flagEnabled.ToString().ToLowerInvariant()}",
                    $"Pantalla: {screenName}",
                    $"Caja consultada: {CajaCodigoLectura}",
                    $"API base URL: {apiBaseUrlState}",
                    $"Token disponible: {tokenAvailable.ToString().ToLowerInvariant()}",
                    $"Consulta iniciada: {queryStarted.ToString().ToLowerInvariant()}",
                    $"HTTP result: {httpResult}",
                    $"Estado final del indicador: {finalVisibility}",
                    $"Mensaje UI asignado: {message}");

                File.AppendAllText(Path.Combine(logDirectory, "caja-api-read.log"), line + Environment.NewLine);
            }
            catch
            {
                // Logging seguro no debe afectar la pantalla.
            }
        }
    }
}
