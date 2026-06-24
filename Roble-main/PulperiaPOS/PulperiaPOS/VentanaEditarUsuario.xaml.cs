using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
using PulperiaPOS.DataAccess;

namespace PulperiaPOS
{
    /// <summary>
    /// Lógica de interacción para VentanaEditarUsuario.xaml
    /// </summary>
    public partial class VentanaEditarUsuario : Window
    {
        public Usuario UsuarioEditado { get; private set; }

        public VentanaEditarUsuario(Usuario usuario = null)
        {
            InitializeComponent();

            if (usuario != null)
            {
                txtNombre.Text = usuario.nombre;
                txtContrasena.Password = usuario.contrasena;
                cmbRol.SelectedItem = cmbRol.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(i => i.Content.ToString() == usuario.rol);

                UsuarioEditado = usuario;
            }
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            string nombre = txtNombre.Text.Trim();
            string contrasena = txtContrasena.Password.Trim();
            string rol = (cmbRol.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(contrasena) || string.IsNullOrEmpty(rol))
            {
                txtError.Text = "Todos los campos son obligatorios.";
                txtError.Visibility = Visibility.Visible;
                return;
            }

            using var conn = DBConnection.GetConnection();
            using var cmd = new SqlCommand("SELECT COUNT(*) FROM usuario WHERE nombre = @nombre AND idUsuario != @id", conn);
            cmd.Parameters.AddWithValue("@nombre", nombre);
            cmd.Parameters.AddWithValue("@id", UsuarioEditado?.idUsuario ?? -1);

            long count = Convert.ToInt64(cmd.ExecuteScalar());

            if (count > 0)
            {
                txtError.Text = "El nombre de usuario ya existe.";
                txtError.Visibility = Visibility.Visible;
                return;
            }

            // Ocultar error si pasa validación
            txtError.Visibility = Visibility.Collapsed;


            if (UsuarioEditado == null)
                UsuarioEditado = new Usuario();

            UsuarioEditado.nombre = nombre;
            UsuarioEditado.contrasena = Seguridad.HashContrasena(contrasena);
            UsuarioEditado.rol = rol;

            MessageBox.Show("✅ Usuario registrado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}