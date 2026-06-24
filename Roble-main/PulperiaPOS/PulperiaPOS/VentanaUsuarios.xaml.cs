using PulperiaPOS.DataAccess;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace PulperiaPOS
{
    /// <summary>
    /// Lógica de interacción para VentanaUsuarios.xaml
    /// </summary>
    public partial class VentanaUsuarios : Window
    {
        private ObservableCollection<Usuario> todosLosUsuarios = new();
        public VentanaUsuarios()
        {
            InitializeComponent();
            CargarUsuarios();
        }

        private void CargarUsuarios()
        {
            todosLosUsuarios.Clear();
            using var conn = DBConnection.GetConnection();
            using var cmd = new SqlCommand("SELECT * FROM usuario", conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                todosLosUsuarios.Add(new Usuario
                {
                    idUsuario = Convert.ToInt32(reader["idUsuario"]),
                    nombre = reader["nombre"].ToString(),
                    contrasena = reader["contrasena"].ToString(),
                    rol = reader["rol"].ToString()
                });
            }

            dgUsuarios.ItemsSource = todosLosUsuarios;
        }


        private void txtBuscar_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtBuscar.Text == "Buscar por nombre o rol")
            {
                txtBuscar.Text = "";
                txtBuscar.Foreground = Brushes.Black;
            }
        }

        private void txtBuscar_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBuscar.Text))
            {
                txtBuscar.Text = "Buscar por nombre o rol";
                txtBuscar.Foreground = Brushes.Gray;
            }
        }

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dgUsuarios == null || txtBuscar == null) return;

            string texto = txtBuscar.Text.ToLower();

            if (string.IsNullOrWhiteSpace(texto) || texto == "buscar por nombre o rol")
            {
                dgUsuarios.ItemsSource = todosLosUsuarios;
            }
            else
            {
                dgUsuarios.ItemsSource = todosLosUsuarios
                    .Where(u => u.nombre.ToLower().Contains(texto) || u.rol.ToLower().Contains(texto))
                    .ToList();
            }
        }


        private void BtnBuscar_Click(object sender, RoutedEventArgs e)
        {
            txtBuscar_TextChanged(null, null);
        }

        private void BtnRefrescar_Click(object sender, RoutedEventArgs e)
        {
            CargarUsuarios();
        }


        private void BtnAgregar_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new VentanaEditarUsuario();
            if (ventana.ShowDialog() == true)
            {
                var nuevoUsuario = ventana.UsuarioEditado;

                using var conn = DBConnection.GetConnection();
                using var cmd = new SqlCommand("INSERT INTO usuario (nombre, contrasena, rol) OUTPUT INSERTED.idUsuario VALUES (@nombre, @contrasena, @rol)", conn);
                cmd.Parameters.AddWithValue("@nombre", nuevoUsuario.nombre);
                cmd.Parameters.AddWithValue("@contrasena", nuevoUsuario.contrasena);
                cmd.Parameters.AddWithValue("@rol", nuevoUsuario.rol);

                nuevoUsuario.idUsuario = Convert.ToInt32(cmd.ExecuteScalar());
                todosLosUsuarios.Add(nuevoUsuario); // Actualiza en tiempo real

                MessageBox.Show("✅ Usuario agregado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }



        private void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            if (dgUsuarios.SelectedItem is Usuario usuarioSeleccionado)
            {
                // Creamos una copia para editar sin afectar directamente el objeto original
                var copia = new Usuario
                {
                    idUsuario = usuarioSeleccionado.idUsuario,
                    nombre = usuarioSeleccionado.nombre,
                    contrasena = usuarioSeleccionado.contrasena,
                    rol = usuarioSeleccionado.rol
                };

                var ventana = new VentanaEditarUsuario(copia);
                if (ventana.ShowDialog() == true)
                {
                    var actualizado = ventana.UsuarioEditado;

                    using var conn = DBConnection.GetConnection();
                    using var cmd = new SqlCommand("UPDATE usuario SET nombre=@nombre, contrasena=@contrasena, rol=@rol WHERE idUsuario=@id", conn);
                    cmd.Parameters.AddWithValue("@nombre", actualizado.nombre);
                    cmd.Parameters.AddWithValue("@contrasena", actualizado.contrasena);
                    cmd.Parameters.AddWithValue("@rol", actualizado.rol);
                    cmd.Parameters.AddWithValue("@id", actualizado.idUsuario);
                    cmd.ExecuteNonQuery();

                    // Actualizamos las propiedades del objeto ya existente en la colección
                    usuarioSeleccionado.nombre = actualizado.nombre;
                    usuarioSeleccionado.contrasena = actualizado.contrasena;
                    usuarioSeleccionado.rol = actualizado.rol;

                    // Refrescar manualmente el DataGrid
                    dgUsuarios.Items.Refresh();

                    MessageBox.Show("✅ Usuario actualizado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                }
            }
        }


        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (dgUsuarios.SelectedItem is Usuario usuarioSeleccionado)
            {
                // Evitar que el usuario actual se elimine a sí mismo
                if (usuarioSeleccionado.nombre.Equals(UserSession.NombreUsuario, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("❌ No puedes eliminar al usuario actualmente en sesión.", "Acción no permitida", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var confirm = MessageBox.Show($"¿Deseas eliminar al usuario '{usuarioSeleccionado.nombre}'?",
                                              "Confirmar eliminación",
                                              MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (confirm == MessageBoxResult.Yes)
                {
                    using var conn = DBConnection.GetConnection();
                    using var cmd = new SqlCommand("DELETE FROM usuario WHERE idUsuario=@id", conn);
                    cmd.Parameters.AddWithValue("@id", usuarioSeleccionado.idUsuario);
                    cmd.ExecuteNonQuery();

                    todosLosUsuarios.Remove(usuarioSeleccionado);

                    MessageBox.Show("✅ Usuario eliminado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }


        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // O navegar a otra ventana si lo deseas
        }

    }

    public class Usuario
    {
        public int idUsuario { get; set; }
        public string nombre { get; set; }
        public string contrasena { get; set; }
        public string rol { get; set; }
    }
}

