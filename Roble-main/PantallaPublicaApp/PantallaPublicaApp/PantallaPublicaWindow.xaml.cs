using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace PantallaPublicaApp
{
    public partial class PantallaPublicaWindow : Window
    {
        // Win32 API para forzar tamaño exacto de ventana
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        static readonly IntPtr HWND_TOP = new IntPtr(0);
        const UInt32 SWP_NOZORDER = 0x0004;
        const UInt32 SWP_NOACTIVATE = 0x0010;

        private List<string> productosIds = new();
        private List<Producto> productosEnPantalla = new();
        private DispatcherTimer refrescoTimer;
        private DispatcherTimer scrollTimer;
        private int indiceScroll = 0;

        private string[] archivosPublicidad;
        private int indicePublicidad = 0;
        private DispatcherTimer publicidadTimer;

        private readonly System.Windows.Forms.Screen monitorSeleccionado;

        public PantallaPublicaWindow(System.Windows.Forms.Screen monitor)
        {
            InitializeComponent();
            monitorSeleccionado = monitor;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            var bounds = monitorSeleccionado.Bounds;

            SetWindowPos(hwnd, HWND_TOP,
                bounds.Left,
                bounds.Top,
                bounds.Width,
                bounds.Height,
                SWP_NOZORDER | SWP_NOACTIVATE);

            this.UpdateLayout();

            CargarIdsProductos();
            CargarProductosDesdeBD();
            IniciarRotacionPublicidad();

            refrescoTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(20)
            };
            refrescoTimer.Tick += (s, ev) => CargarProductosDesdeBD();
            refrescoTimer.Start();
        }

        private void CargarIdsProductos()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "productos_pantalla.json");
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                productosIds = JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
            }
        }

        private void CargarProductosDesdeBD()
        {
            if (productosIds == null || productosIds.Count == 0)
                return;

            using var conn = DBConnection.GetConnection();
            var lista = new List<Producto>();

            foreach (var id in productosIds)
            {
                var cmd = new SqlCommand("SELECT nombre, precio, stock FROM inventario WHERE idProducto = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    int stock = Convert.ToInt32(reader["stock"]);
                    if (stock > 0)
                    {
                        lista.Add(new Producto
                        {
                            IdProducto = id,
                            Nombre = reader["nombre"].ToString(),
                            Precio = Convert.ToDouble(reader["precio"]),
                            Stock = stock
                        });
                    }
                }
                reader.Close();
            }

            productosEnPantalla = lista;
            ConstruirVistaProductos();
            Dispatcher.BeginInvoke(new Action(() =>
            {
                IniciarAnimacionDesplazamiento();
            }), DispatcherPriority.Background);
        }

        private void ConstruirVistaProductos()
        {
            StackProductos.Children.Clear();

            foreach (var p in productosEnPantalla)
            {
                StackProductos.Children.Add(CrearItemProducto(p));
            }

            foreach (var p in productosEnPantalla)
            {
                StackProductos.Children.Add(CrearItemProducto(p));
            }
        }

        private StackPanel CrearItemProducto(Producto p)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 10, 0, 10) };

            panel.Children.Add(new TextBlock
            {
                Text = p.Nombre,
                FontWeight = FontWeights.Bold,
                FontSize = 24
            });

            panel.Children.Add(new TextBlock
            {
                Text = $"₡{p.Precio:N0}",
                FontSize = 20
            });

            panel.Children.Add(new TextBlock
            {
                Text = $"Disponibles: {p.Stock}",
                FontSize = 18
            });

            return panel;
        }

        private void IniciarAnimacionDesplazamiento()
        {
            scrollTimer?.Stop();

            if (productosEnPantalla.Count <= 5)
                return;

            double itemHeight = ((FrameworkElement)StackProductos.Children[0]).ActualHeight + 20;
            double alturaOriginal = itemHeight * productosEnPantalla.Count;

            double velocidad = 1;
            scrollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(30)
            };

            scrollTimer.Tick += (s, e) =>
            {
                double nuevoOffset = ProductosTransform.Y - velocidad;

                if (Math.Abs(nuevoOffset) >= alturaOriginal)
                {
                    ProductosTransform.Y = 0;
                }
                else
                {
                    ProductosTransform.Y = nuevoOffset;
                }
            };

            scrollTimer.Start();
        }

        private void IniciarRotacionPublicidad()
        {
            string carpeta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Publicidad");
            if (!Directory.Exists(carpeta))
                Directory.CreateDirectory(carpeta);

            archivosPublicidad = Directory.GetFiles(carpeta, "*.*")
                .Where(f => f.EndsWith(".jpg") || f.EndsWith(".png") || f.EndsWith(".mp4") || f.EndsWith(".avi"))
                .ToArray();

            if (archivosPublicidad.Length == 0)
                return;

            publicidadTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
            publicidadTimer.Tick += (s, e) => MostrarSiguientePublicidad();
            MostrarSiguientePublicidad();
            publicidadTimer.Start();
        }

        private void MostrarSiguientePublicidad()
        {
            if (archivosPublicidad.Length == 0)
                return;

            string archivo = archivosPublicidad[indicePublicidad];
            indicePublicidad = (indicePublicidad + 1) % archivosPublicidad.Length;

            ImagenPublicidad.Visibility = Visibility.Collapsed;
            VideoPublicidad.Visibility = Visibility.Collapsed;
            VideoPublicidad.Stop();

            if (archivo.EndsWith(".jpg") || archivo.EndsWith(".png"))
            {
                ImagenPublicidad.Source = new BitmapImage(new Uri(archivo));
                ImagenPublicidad.Visibility = Visibility.Visible;
                publicidadTimer.Start();
            }
            else if (archivo.EndsWith(".mp4") || archivo.EndsWith(".avi"))
            {
                publicidadTimer.Stop();
                VideoPublicidad.Source = new Uri(archivo);
                VideoPublicidad.Visibility = Visibility.Visible;
                VideoPublicidad.Play();
            }
        }

        private void VideoPublicidad_MediaEnded(object sender, RoutedEventArgs e)
        {
            MostrarSiguientePublicidad();
        }
    }
}
