using PulperiaPOS.DataAccess;
using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace PulperiaPOS
{
    public partial class ClientePage : Window
    {
        ObservableCollection<Cliente> clientes = new ObservableCollection<Cliente>();

        public ClientePage()
        {
            InitializeComponent();
            CargarClientes();

            if (UserSession.RolUsuario == "Anfitrion")
            {
                BtnAgregar.Visibility = Visibility.Collapsed;
                BtnEditar.Visibility = Visibility.Collapsed;
                BtnEliminar.Visibility = Visibility.Collapsed;
            }
        }

        private void CargarClientes()
        {
            clientes.Clear();
            using var conn = DBConnection.GetConnection();
            var cmd = new SqlCommand("SELECT * FROM cliente", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                clientes.Add(new Cliente
                {
                    idCliente = reader.GetInt32(0),
                    nombre = reader.GetString(1),
                    saldo = Convert.ToDouble(reader["saldo"]),
                    comprobante = reader.IsDBNull(3) ? "" : reader.GetString(3)
                });
            }
            dataGridClientes.ItemsSource = clientes;
        }

        private void BtnAgregar_Click(object sender, RoutedEventArgs e)
        {
            var form = new ClienteForm();
            if (form.ShowDialog() == true)
                CargarClientes();
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridClientes.SelectedItem is Cliente seleccionado)
            {
                var form = new ClienteForm(seleccionado);
                if (form.ShowDialog() == true)
                    CargarClientes();
            }
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridClientes.SelectedItem is Cliente seleccionado)
            {
                // ❌ No permitir eliminar al Cliente General
                if (seleccionado.idCliente == 0)
                {
                    MessageBox.Show("No puedes eliminar al Cliente General.", "Operación no permitida", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show("¿Deseas eliminar este cliente?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes)
                    return;

                using var conn = DBConnection.GetConnection();

                // Verificar si tiene ventas
                using (var cmdCheckVentas = new SqlCommand("SELECT COUNT(*) FROM ventas WHERE cliente_id = @id", conn))
                {
                    cmdCheckVentas.Parameters.AddWithValue("@id", seleccionado.idCliente);
                    int ventasRelacionadas = (int)cmdCheckVentas.ExecuteScalar();
                    if (ventasRelacionadas > 0)
                    {
                        MessageBox.Show("No se puede eliminar el cliente porque tiene ventas registradas.", "Operación bloqueada", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                // Verificar si tiene registros en saldo_liberado
                using (var cmdCheckSaldo = new SqlCommand("SELECT COUNT(*) FROM saldo_liberado WHERE idCliente = @id", conn))
                {
                    cmdCheckSaldo.Parameters.AddWithValue("@id", seleccionado.idCliente);
                    int saldosRelacionados = (int)cmdCheckSaldo.ExecuteScalar();
                    if (saldosRelacionados > 0)
                    {
                        MessageBox.Show("No se puede eliminar el cliente porque tiene registros en el historial de saldo liberado.", "Operación bloqueada", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                // Si pasa las validaciones, eliminar
                using (var cmd = new SqlCommand("DELETE FROM cliente WHERE idCliente = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", seleccionado.idCliente);
                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Cliente eliminado correctamente.");
                CargarClientes();
            }
        }




        private void BtnBuscar_Click(object sender, RoutedEventArgs e)
        {
            string nombreBuscar = txtBuscar.Text.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(nombreBuscar))
            {
                CargarClientes();
                return;
            }

            clientes.Clear();
            using var conn = DBConnection.GetConnection();
            var cmd = new SqlCommand("SELECT * FROM cliente WHERE LOWER(nombre) LIKE @nombre", conn);
            cmd.Parameters.AddWithValue("@nombre", $"%{nombreBuscar}%");
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                clientes.Add(new Cliente
                {
                    idCliente = reader.GetInt32(0),
                    nombre = reader.GetString(1),
                    saldo = Convert.ToDouble(reader["saldo"]),
                    comprobante = reader.IsDBNull(3) ? "" : reader.GetString(3)
                });
            }

            if (clientes.Count == 0)
                MessageBox.Show("No se encontraron clientes con ese nombre.");
        }

        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnLiberarSaldo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Cliente cliente)
            {
                if (cliente.saldo <= 0)
                {
                    MessageBox.Show("Este cliente no tiene saldo para liberar.");
                    return;
                }

                var confirm = MessageBox.Show($"¿Deseas liberar ₡{cliente.saldo:N2} del cliente '{cliente.nombre}' a favor del punto de venta?",
                                              "Confirmar Liberación",
                                              MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes) return;

                string fecha = DateTime.Now.ToString("yyyy-MM-dd");
                string motivo = $"Saldo no utilizado por el cliente {cliente.nombre}";

                using var conn = DBConnection.GetConnection();
                using var trans = conn.BeginTransaction();

                try
                {
                    // Elimina esta creación si ya existe en tu modelo de base de datos en Azure.
                    /*
                    var crearTablaCmd = new SqlCommand(@"
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='saldo_liberado' AND xtype='U')
                        CREATE TABLE saldo_liberado (
                            idLiberacion INT IDENTITY(1,1) PRIMARY KEY,
                            idCliente INT NOT NULL,
                            monto FLOAT NOT NULL,
                            fecha DATE NOT NULL,
                            motivo NVARCHAR(255),
                            FOREIGN KEY (idCliente) REFERENCES cliente(idCliente)
                        );", conn, trans);
                    crearTablaCmd.ExecuteNonQuery();
                    */

                    var insertCmd = new SqlCommand(@"
                        INSERT INTO saldo_liberado (idCliente, monto, fecha, motivo)
                        VALUES (@idCliente, @monto, @fecha, @motivo);", conn, trans);
                    insertCmd.Parameters.AddWithValue("@idCliente", cliente.idCliente);
                    insertCmd.Parameters.AddWithValue("@monto", cliente.saldo);
                    insertCmd.Parameters.AddWithValue("@fecha", fecha);
                    insertCmd.Parameters.AddWithValue("@motivo", motivo);
                    insertCmd.ExecuteNonQuery();

                    var updateCmd = new SqlCommand("UPDATE cliente SET saldo = 0 WHERE idCliente = @id", conn, trans);
                    updateCmd.Parameters.AddWithValue("@id", cliente.idCliente);
                    updateCmd.ExecuteNonQuery();

                    trans.Commit();

                    MessageBox.Show("Saldo liberado correctamente.");
                    CargarClientes();
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    MessageBox.Show("Error al liberar saldo: " + ex.Message);
                }
            }
        }


        private void BtnReporteClientesSaldo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var clientesConSaldo = new List<Cliente>();

                using (var conn = DBConnection.GetConnection())
                {
                    var cmd = new SqlCommand("SELECT nombre, saldo FROM cliente WHERE saldo > 0 ORDER BY nombre", conn);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            clientesConSaldo.Add(new Cliente
                            {
                                nombre = reader.GetString(0),
                                saldo = Convert.ToDouble(reader.GetValue(1))
                            });
                        }
                    }
                }

                if (clientesConSaldo.Count == 0)
                {
                    MessageBox.Show("No hay clientes con saldo disponible.");
                    return;
                }

                // Generar archivo
                string nombreArchivo = $"ReporteClientesSaldo_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string ruta = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), nombreArchivo);

                using (var writer = new System.IO.StreamWriter(ruta, false, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine("REPORTE DE CLIENTES CON SALDO DISPONIBLE");
                    writer.WriteLine("Generado el  : " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                    writer.WriteLine("------------------------------------------");

                    foreach (var cliente in clientesConSaldo)
                    {
                        writer.WriteLine($"{cliente.nombre,-30} ₡{cliente.saldo:N2}");
                    }

                    writer.WriteLine("------------------------------------------");
                    writer.WriteLine($"Total clientes: {clientesConSaldo.Count}");
                }

                MessageBox.Show("Reporte generado exitosamente en el escritorio:\n" + nombreArchivo);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar el reporte: " + ex.Message);
            }
        }


        private void BtnHistorialLiberaciones_Click(object sender, RoutedEventArgs e)
        {
            var historial = new SaldoLiberadoPage();
            historial.ShowDialog();
        }
    }
}
