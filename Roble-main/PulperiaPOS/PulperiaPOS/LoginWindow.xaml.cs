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
using PulperiaPOS.ApiClients;
using PulperiaPOS.Configuration;
using PulperiaPOS.DataAccess;
using PulperiaPOS.Models.Auth;

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
        private async void BtnIngresar_Click(object sender, RoutedEventArgs e)
        {
            string usuario = txtUsuario.Text;
            string contrasena = txtContrasena.Password;

            if (FeatureFlags.UseApiLogin)
            {
                await IngresarConApiAsync(usuario, contrasena);
                return;
            }

            var (id, rol, nombre) = ObtenerDatosUsuario(usuario, contrasena);

            // Guardar en sesión
            UserSession.Clear();
            ApiSessionCoordinator.Reset();
            UserSession.IdUsuario = id;
            UserSession.NombreUsuario = nombre;
            UserSession.RolUsuario = rol;

            AbrirVentanaPorRol(rol, nombre);
        }

        private async Task IngresarConApiAsync(string usuario, string contrasena)
        {
            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(contrasena))
            {
                MessageBox.Show("Usuario o contraseña incorrectos", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using var authClient = new AuthApiClient();
            var result = await authClient.LoginAsync(usuario, contrasena);
            if (!result.Success || result.Response?.User is null)
            {
                MostrarErrorLoginApi(result.Failure);
                return;
            }

            var response = result.Response;
            var user = response.User;

            UserSession.Clear();
            ApiSessionCoordinator.Reset();
            UserSession.IdUsuario = user.Id;
            UserSession.NombreUsuario = user.Username;
            UserSession.RolUsuario = user.Role;
            UserSession.IsApiAuthenticated = true;
            UserSession.AccessToken = response.AccessToken;
            UserSession.TokenExpiresAtUtc = response.ExpiresAtUtc;
            UserSession.Permissions = user.Permissions ?? Array.Empty<string>();

            AbrirVentanaPorRol(user.Role, user.Username);
        }

        private void AbrirVentanaPorRol(string rol, string nombre)
        {
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

        private static void MostrarErrorLoginApi(AuthApiFailure failure)
        {
            var message = failure switch
            {
                AuthApiFailure.RateLimited => "Se alcanzó el límite de intentos. Espera un momento e intenta nuevamente.",
                AuthApiFailure.ServiceUnavailable => "El servicio de autenticación no está disponible.",
                AuthApiFailure.NetworkError => "No se pudo conectar con el servicio de autenticación.",
                AuthApiFailure.ConfigurationError => "La autenticación por API no está configurada correctamente.",
                AuthApiFailure.InvalidResponse => "No se pudo validar la respuesta de autenticación.",
                _ => "Usuario o contraseña incorrectos"
            };

            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
