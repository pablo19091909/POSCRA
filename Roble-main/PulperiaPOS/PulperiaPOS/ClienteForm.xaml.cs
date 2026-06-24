using System;
using System.Data.SqlClient;
using System.Windows;
using PulperiaPOS.DataAccess;

namespace PulperiaPOS
{
    /// <summary>
    /// Lógica de interacción para ClienteForm.xaml
    /// </summary>
    public partial class ClienteForm : Window
    {
        private Cliente clienteActual;
        private bool esEdicion;

        public ClienteForm()
        {
            InitializeComponent();
            esEdicion = false;
        }

        public ClienteForm(Cliente cliente) : this()
        {
            clienteActual = cliente;
            esEdicion = true;
            txtNombre.Text = cliente.nombre;
            txtSaldo.Text = cliente.saldo.ToString("F2");
            txtComprobante.Text = cliente.comprobante;
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            string nombre = txtNombre.Text.Trim();
            string comprobante = txtComprobante.Text.Trim();

            if (string.IsNullOrWhiteSpace(nombre))
            {
                MessageBox.Show("El nombre del cliente es obligatorio.");
                return;
            }

            if (!double.TryParse(txtSaldo.Text, out double saldo) || saldo < 0)
            {
                MessageBox.Show("Por favor ingrese un saldo válido (mayor o igual a 0).");
                return;
            }

            using var conn = DBConnection.GetConnection();

            // Registrar la fecha solo si hay comprobante válido
            string fechaCarga = string.IsNullOrWhiteSpace(comprobante)
                ? null
                : DateTime.Now.ToString("yyyy-MM-dd");

            if (esEdicion)
            {
                var cmd = new SqlCommand(@"
                    UPDATE cliente 
                    SET nombre = @nombre, 
                        saldo = @saldo, 
                        comprobante = @comprobante, 
                        fecha_carga_saldo = @fecha 
                    WHERE idCliente = @id", conn);

                cmd.Parameters.AddWithValue("@nombre", nombre);
                cmd.Parameters.AddWithValue("@saldo", saldo);
                cmd.Parameters.AddWithValue("@comprobante", comprobante);
                cmd.Parameters.AddWithValue("@fecha", (object?)fechaCarga ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", clienteActual.idCliente);
                cmd.ExecuteNonQuery();
            }
            else
            {
                var cmd = new SqlCommand(@"
                    INSERT INTO cliente (nombre, saldo, comprobante, fecha_carga_saldo) 
                    VALUES (@nombre, @saldo, @comprobante, @fecha)", conn);

                cmd.Parameters.AddWithValue("@nombre", nombre);
                cmd.Parameters.AddWithValue("@saldo", saldo);
                cmd.Parameters.AddWithValue("@comprobante", comprobante);
                cmd.Parameters.AddWithValue("@fecha", (object?)fechaCarga ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }

            DialogResult = true;
            Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
