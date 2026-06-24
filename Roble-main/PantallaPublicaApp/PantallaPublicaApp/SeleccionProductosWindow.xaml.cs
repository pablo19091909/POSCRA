using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;



namespace PantallaPublicaApp
{
    /// <summary>
    /// Lógica de interacción para SeleccionProductosWindow.xaml
    /// </summary>
    public partial class SeleccionProductosWindow : Window
    {
        public List<Producto> ProductosSeleccionados { get; private set; } = new();

        public SeleccionProductosWindow()
        {
            InitializeComponent();
            CargarProductos();

        }

        private List<Producto> listaCompleta = new List<Producto>();

        private void CargarProductos()
        {
            // Cargar IDs ya seleccionados (desde archivo JSON)
            List<string> productosYaSeleccionados = new();
            string jsonPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "productos_pantalla.json");

            if (File.Exists(jsonPath))
            {
                string json = File.ReadAllText(jsonPath);
                productosYaSeleccionados = JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
            }

            // Cargar productos desde la base
            var lista = new List<Producto>();
            using var conn = DBConnection.GetConnection();
            var cmd = new SqlCommand("SELECT idProducto, nombre FROM inventario", conn);
            var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string id = reader["idProducto"].ToString();
                lista.Add(new Producto
                {
                    IdProducto = id,
                    Nombre = reader["nombre"].ToString(),
                    Seleccionado = productosYaSeleccionados.Contains(id)
                });
            }

            listaCompleta = lista;
            dataGridProductos.ItemsSource = lista;
        }


        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            ProductosSeleccionados.Clear();
            foreach (var item in dataGridProductos.Items)
            {
                if (item is Producto p && p.Seleccionado)
                    ProductosSeleccionados.Add(p);
            }

            if (ProductosSeleccionados.Count == 0)
            {
                System.Windows.MessageBox.Show("No has seleccionado productos.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }

            // Guardar solo los IDs en el JSON
            var ids = ProductosSeleccionados.Select(p => p.IdProducto).ToList();
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(ids, Newtonsoft.Json.Formatting.Indented);
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "productos_pantalla.json");
            File.WriteAllText(path, json);

            System.Windows.MessageBox.Show($"{ids.Count} productos guardados para pantalla.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }
        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (listaCompleta == null || listaCompleta.Count == 0)
                return;

            string filtro = txtBuscar.Text.Trim().ToLower();

            var filtrado = listaCompleta.Where(p =>
                p.Nombre.ToLower().Contains(filtro) ||
                p.IdProducto.ToLower().Contains(filtro)).ToList();

            dataGridProductos.ItemsSource = filtrado;
        }

        private void BtnLimpiarBuscar_Click(object sender, RoutedEventArgs e)
        {
            txtBuscar.Text = "";
            dataGridProductos.ItemsSource = listaCompleta;
        }



    }
}
