using System.IO;
using System.Windows;
using WpfApp = System.Windows;
using WinForms = System.Windows.Forms;

namespace PantallaPublicaApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            System.Diagnostics.Debug.WriteLine("🛠️ Constructor de MainWindow iniciado");

            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("📐 InitializeComponent ejecutado");

            try
            {
                foreach (var screen in WinForms.Screen.AllScreens)
                {
                    cmbMonitores.Items.Add(screen.DeviceName);
                }

                if (cmbMonitores.Items.Count > 0)
                    cmbMonitores.SelectedIndex = 0;

                System.Diagnostics.Debug.WriteLine("📋 Combo de monitores cargado");
            }
            catch (Exception ex)
            {
                File.AppendAllText("error_log.txt", $"[{DateTime.Now}] Error en constructor de MainWindow:\n{ex}\n");
                WpfApp.MessageBox.Show($"Error cargando monitores:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Forzar visibilidad
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.WindowState = WindowState.Normal;
            this.Visibility = Visibility.Visible;
            this.Left = 100;
            this.Top = 100;
            this.Topmost = true;
            this.Focus();
            this.Activate();
            this.ShowInTaskbar = true;

            // Eventos para rastreo
            this.Loaded += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine("📥 MainWindow.Loaded");
            };

            this.ContentRendered += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine("🖼️ MainWindow.ContentRendered");
            };

            this.Closing += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine("🔁 MainWindow.Closing");
            };

            this.Closed += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine("❌ MainWindow se cerró");
            };
        }


        private void BtnSeleccionarProductos_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                var seleccionador = new SeleccionProductosWindow
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Topmost = true
                };

                seleccionador.Closed += (s, args) =>
                {
                    this.WindowState = WindowState.Normal;
                    this.Activate();
                };

                seleccionador.Show(); // o ShowDialog() si querés bloquear
                seleccionador.Focus();
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }






        private void BtnAbrirPantallaPublica_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int monitorIndex = cmbMonitores.SelectedIndex;
                if (monitorIndex < 0) monitorIndex = 0;

                var screen = WinForms.Screen.AllScreens[monitorIndex];

                var pantalla = new PantallaPublicaWindow(screen); // ✅ pasamos el monitor

                pantalla.WindowStartupLocation = WindowStartupLocation.Manual;
                pantalla.WindowStyle = WindowStyle.None;
                pantalla.ResizeMode = ResizeMode.NoResize;
                pantalla.Topmost = true;
                pantalla.ShowInTaskbar = true;

                pantalla.Show();
            }
            catch (Exception ex)
            {
                WpfApp.MessageBox.Show($"No se pudo abrir la pantalla pública:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void BtnCargarPublicidad_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Multimedia|*.jpg;*.png;*.mp4;*.avi",
                Multiselect = true,
                Title = "Seleccionar imágenes o videos para pantalla"
            };

            if (dialog.ShowDialog() == true)
            {
                string destino = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Publicidad");

                if (!Directory.Exists(destino))
                    Directory.CreateDirectory(destino);

                foreach (var archivo in dialog.FileNames)
                {
                    string nombreArchivo = Path.GetFileName(archivo);
                    string rutaDestino = Path.Combine(destino, nombreArchivo);
                    File.Copy(archivo, rutaDestino, true);
                }

                WpfApp.MessageBox.Show("Archivos cargados correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnSalir_Click(object sender, RoutedEventArgs e)
        {
            WpfApp.Application.Current.Shutdown();
        }
    }
}
