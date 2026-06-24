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
    /// Lógica de interacción para LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
         
           

        }
        private void BtnIngresar_Click(object sender, RoutedEventArgs e)
        {
            string usuario = txtUsuario.Text;
            string contrasena = txtContrasena.Password;

            var (id, rol, nombre) = ObtenerDatosUsuario(usuario, contrasena);

            // Guardar en sesión
            UserSession.IdUsuario = id;
            UserSession.NombreUsuario = nombre;
            UserSession.RolUsuario = rol;

            if (rol == "Administrador")
            {
                var ventanaAdmin = new VentanaAdministrador(nombre);
                ventanaAdmin.Show();
                this.Close();
            }
            else if (rol == "Anfitrion")
            {
                var ventanaAnfitrion = new VentanaAnfitrion(nombre);
                ventanaAnfitrion.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Usuario o contraseña incorrectos", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private (int id, string rol, string nombre) ObtenerDatosUsuario(string usuario, string contrasena)
        {
            try
            {
                string hash = Seguridad.HashContrasena(contrasena);

                using var conn = DBConnection.GetConnection();
                using var cmd = new SqlCommand("SELECT idUsuario, rol, nombre FROM usuario WHERE nombre = @usuario AND contrasena = @contrasena", conn);
                cmd.Parameters.AddWithValue("@usuario", usuario);
                cmd.Parameters.AddWithValue("@contrasena", hash);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return (
                        reader.GetInt32(0),               // idUsuario
                        reader["rol"].ToString(),         // rol
                        reader["nombre"].ToString()       // nombre
                    );
                }
                else
                {
                    return (0, string.Empty, string.Empty);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al verificar las credenciales: " + ex.Message);
                return (0, string.Empty, string.Empty);
            }
        }

    }
}
