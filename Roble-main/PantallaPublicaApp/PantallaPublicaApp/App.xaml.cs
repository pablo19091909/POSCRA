using System;
using System.Data.SqlClient;
using System.IO;
using System.Windows;

namespace PantallaPublicaApp
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown; // ✅ Esto mantiene la app viva
            System.Diagnostics.Debug.WriteLine("🚀 App iniciando...");

            var ventanaCarga = new VentanaCarga();
            ventanaCarga.Show();
            ventanaCarga.UpdateLayout();
            System.Diagnostics.Debug.WriteLine("🔄 Mostrada ventana de carga");

            if (!ProbarConexion())
            {
                ventanaCarga.Close();
                System.Windows.MessageBox.Show("No se pudo establecer conexión con la base de datos.\nVerifique su conexión o configuración.",
                                "Error de conexión",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                Current.Shutdown();
                return;
            }

            System.Diagnostics.Debug.WriteLine("✅ Conexión exitosa");

            try
            {
                ventanaCarga.Close();
                var main = new MainWindow();
                System.Diagnostics.Debug.WriteLine("📦 MainWindow instanciada");
                main.Show();
                this.MainWindow = main; // ✅ esto es lo correcto en OnStartup
                System.Diagnostics.Debug.WriteLine("🟢 MainWindow mostrada");
            }

            catch (Exception ex)
            {
                File.AppendAllText("error_log.txt", $"[{DateTime.Now}] Error en MainWindow:\n{ex}\n");
                System.Windows.MessageBox.Show($"Error al iniciar MainWindow:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }


            // ⚠️ Rastreo si alguien cierra la app globalmente
            Current.Exit += (s, ev) =>
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Application.Exit triggered");
            };
        }

        private bool ProbarConexion()
        {
            try
            {
                using var conn = DBConnection.GetConnection();
                return true;
            }
            catch (Exception ex)
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");
                File.AppendAllText(logPath, $"[{DateTime.Now}] Error de conexión: {ex.Message}\n");
                System.Diagnostics.Debug.WriteLine($"❌ Conexión fallida: {ex.Message}");
                return false;
            }
        }
    }
}
