using PulperiaPOS.ApiClients;
using PulperiaPOS.Configuration;
using PulperiaPOS.DataAccess;
using PulperiaPOS.Models.Api;
using PulperiaPOS.Models.Clientes;
using PulperiaPOS.Models.Productos;
using PulperiaPOS.Views;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing.Printing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;




namespace PulperiaPOS
{
    public partial class VentasPage : Window
    {
        private List<ProductoVenta> productosEnVenta = new();
        private bool limpiarTextoCliente = true;


        public VentasPage()
        {
            InitializeComponent();
            CargarClientes();
            CargarMetodoPago();
            ActualizarTotal();
            this.Loaded += VentasPage_Loaded;


        }

        private List<Cliente> todosLosClientes = new List<Cliente>();
        public List<Cliente> ClientesFiltrados { get; set; } = new List<Cliente>();

        private async void CargarClientes()
        {
            if (FeatureFlags.UseVentasClienteSelectorApi)
            {
                await CargarClientesDesdeApiAsync(null);
                return;
            }

            CargarClientesDesdeSql();
        }

        private void CargarClientesDesdeSql()
        {
            todosLosClientes.Clear();
            ClientesFiltrados.Clear();

            try
            {
                using var connection = DBConnection.GetConnection();
                string query = "SELECT idCliente, nombre FROM cliente";
                using var command = new SqlCommand(query, connection);
                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    todosLosClientes.Add(new Cliente
                    {
                        idCliente = Convert.ToInt32(reader["idCliente"]),
                        nombre = reader["nombre"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar los clientes: {ex.Message}");
            }

            if (!todosLosClientes.Any(c => c.nombre == "Cliente General"))
            {
                todosLosClientes.Insert(0, new Cliente { idCliente = 0, nombre = "Cliente General" });
            }

            ClientesFiltrados = new List<Cliente>(todosLosClientes);
            ClienteComboBox.ItemsSource = ClientesFiltrados;
            ClienteComboBox.SelectedIndex = 0;
        }

        private async System.Threading.Tasks.Task CargarClientesDesdeApiAsync(string? busqueda)
        {
            todosLosClientes.Clear();
            ClientesFiltrados.Clear();

            using var client = new ClientesApiClient();
            var result = await client.GetClientesAsync(busqueda);
            if (!result.Success)
            {
                MostrarErrorClientesApi(result);
                ClienteComboBox.ItemsSource = ClientesFiltrados;
                ClienteComboBox.SelectedIndex = -1;
                return;
            }

            foreach (var cliente in result.Data ?? Array.Empty<ClienteListItemResponse>())
            {
                todosLosClientes.Add(new Cliente
                {
                    idCliente = cliente.IdCliente,
                    nombre = cliente.Nombre,
                    saldo = Convert.ToDouble(cliente.Saldo),
                    comprobante = cliente.Comprobante
                });
            }

            if (!todosLosClientes.Any(c => c.nombre == "Cliente General"))
            {
                todosLosClientes.Insert(0, new Cliente { idCliente = 0, nombre = "Cliente General" });
            }

            ClientesFiltrados = new List<Cliente>(todosLosClientes);
            ClienteComboBox.ItemsSource = ClientesFiltrados;
            ClienteComboBox.SelectedIndex = string.IsNullOrWhiteSpace(busqueda) && ClientesFiltrados.Count > 0 ? 0 : -1;
        }

        private static void MostrarErrorClientesApi<T>(ApiRequestResult<T> result)
        {
            if (result.ErrorType is ApiErrorType.Unauthorized or ApiErrorType.SessionExpired)
            {
                return;
            }

            MessageBox.Show(result.Message, "Clientes", MessageBoxButton.OK, MessageBoxImage.Warning);
        }




        private ComboBoxItem saldoClienteItem;

        private void CargarMetodoPago()
        {
            MetodoPagoComboBox.Items.Clear();
            MetodoPagoComboBox.Items.Add(new ComboBoxItem { Content = "Efectivo" });
            MetodoPagoComboBox.Items.Add(new ComboBoxItem { Content = "Tarjeta" });
            MetodoPagoComboBox.Items.Add(new ComboBoxItem { Content = "Sinpe" });
            MetodoPagoComboBox.Items.Add(new ComboBoxItem { Content = "Dólares" });


            saldoClienteItem = new ComboBoxItem { Content = "Saldo Cliente" };
            MetodoPagoComboBox.Items.Add(saldoClienteItem);

            MetodoPagoComboBox.SelectedIndex = 0;
        }


        private void BuscarProductoTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AgregarProductoPorCodigo();
                e.Handled = true; // para evitar sonidos del sistema
            }
        }

        private void VentasPage_Loaded(object sender, RoutedEventArgs e)
        {
            BuscarProductoTextBox.Focus(); // Coloca el foco en el textbox al abrir
        }

        private async void AgregarProductoPorCodigo()
        {
            string textoBusqueda = BuscarProductoTextBox.Text.Trim();

            if (string.IsNullOrEmpty(textoBusqueda))
            {
                MessageBox.Show("Por favor ingrese un nombre o código de producto.");
                return;
            }

            if (FeatureFlags.UseVentasProductosApi)
            {
                await AgregarProductoDesdeApiAsync(textoBusqueda, acumularExistente: true);
                return;
            }

            try
            {
                using var connection = DBConnection.GetConnection();
                string query = "SELECT idProducto, nombre, precio, stock FROM inventario WHERE idProducto = @busqueda OR nombre LIKE @busquedaNombre";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@busqueda", textoBusqueda);
                command.Parameters.AddWithValue("@busquedaNombre", $"%{textoBusqueda}%");

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    int stock = Convert.ToInt32(reader["stock"]);
                    if (stock <= 0)
                    {
                        MessageBox.Show("El producto no tiene stock disponible.");
                        return;
                    }

                    string idProducto = reader["idProducto"].ToString();

                    var productoExistente = productosEnVenta.FirstOrDefault(p => p.IdProducto == idProducto);
                    if (productoExistente != null)
                    {
                        if (productoExistente.Cantidad + 1 > stock)
                        {
                            MessageBox.Show("No hay suficiente stock disponible para agregar otra unidad.");
                            return;
                        }

                        productoExistente.Cantidad += 1;
                    }
                    else
                    {
                        double precio = reader["precio"] is decimal dec ? Convert.ToDouble(dec) : Convert.ToDouble(reader["precio"]);

                        var producto = new ProductoVenta
                        {
                            IdProducto = idProducto,
                            Nombre = reader["nombre"].ToString(),
                            PrecioUnitario = precio,
                            Cantidad = 1,
                            StockDisponible = stock
                        };

                        productosEnVenta.Add(producto);
                    }

                    ActualizarDataGrid();
                    ActualizarTotal();
                    BuscarProductoTextBox.Clear();
                    BuscarProductoTextBox.Focus();
                    ActualizarVisualSaldoCliente();
                }
                else
                {
                    MessageBox.Show("Producto no encontrado.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al buscar el producto: {ex.Message}\n{ex.StackTrace}");
            }
        }



        private async void AgregarProducto_Click(object sender, RoutedEventArgs e)
        {
            string textoBusqueda = BuscarProductoTextBox.Text.Trim();

            if (string.IsNullOrEmpty(textoBusqueda))
            {
                MessageBox.Show("Por favor ingrese un nombre o código de producto.");
                return;
            }

            if (FeatureFlags.UseVentasProductosApi)
            {
                await AgregarProductoDesdeApiAsync(textoBusqueda, acumularExistente: false);
                return;
            }

            try
            {
                using var connection = DBConnection.GetConnection();
                string query = "SELECT idProducto, nombre, precio, stock FROM inventario WHERE idProducto = @busqueda OR nombre LIKE @busquedaNombre";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@busqueda", textoBusqueda);
                command.Parameters.AddWithValue("@busquedaNombre", $"%{textoBusqueda}%");

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    int stock = Convert.ToInt32(reader["stock"]);
                    if (stock <= 0)
                    {
                        MessageBox.Show("El producto no tiene stock disponible.");
                        return;
                    }

                    double precio = reader["precio"] is decimal dec ? Convert.ToDouble(dec) : Convert.ToDouble(reader["precio"]);

                    var producto = new ProductoVenta
                    {
                        IdProducto = reader["idProducto"].ToString(),
                        Nombre = reader["nombre"].ToString(),
                        PrecioUnitario = precio,
                        Cantidad = 1,
                        StockDisponible = stock
                    };

                    productosEnVenta.Add(producto);
                    ActualizarDataGrid();
                    ActualizarTotal();
                    BuscarProductoTextBox.Clear();
                    BuscarProductoTextBox.Focus();
                    ActualizarVisualSaldoCliente();
                }
                else
                {
                    MessageBox.Show("Producto no encontrado.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al buscar el producto: {ex.Message}\n{ex.StackTrace}");
            }
        }





        private async System.Threading.Tasks.Task AgregarProductoDesdeApiAsync(string textoBusqueda, bool acumularExistente)
        {
            using var client = new ProductosApiClient();
            var result = await client.BuscarPrimeroParaVentaAsync(textoBusqueda);
            if (!result.Success)
            {
                MostrarErrorProductosApi(result);
                return;
            }

            if (result.Data is null)
            {
                MessageBox.Show("Producto no encontrado.");
                return;
            }

            AgregarProductoApiAlCarrito(result.Data, acumularExistente);
        }

        private void AgregarProductoApiAlCarrito(ProductoVentaApiResponse productoApi, bool acumularExistente)
        {
            int stock = productoApi.StockDisponible;
            if (stock <= 0)
            {
                MessageBox.Show("El producto no tiene stock disponible.");
                return;
            }

            var productoExistente = productosEnVenta.FirstOrDefault(p => p.IdProducto == productoApi.IdProducto);
            if (acumularExistente && productoExistente != null)
            {
                if (productoExistente.Cantidad + 1 > stock)
                {
                    MessageBox.Show("No hay suficiente stock disponible para agregar otra unidad.");
                    return;
                }

                productoExistente.Cantidad += 1;
            }
            else
            {
                var producto = new ProductoVenta
                {
                    IdProducto = productoApi.IdProducto,
                    Nombre = productoApi.Nombre,
                    PrecioUnitario = Convert.ToDouble(productoApi.Precio),
                    Cantidad = 1,
                    StockDisponible = stock
                };

                productosEnVenta.Add(producto);
            }

            ActualizarDataGrid();
            ActualizarTotal();
            BuscarProductoTextBox.Clear();
            BuscarProductoTextBox.Focus();
            ActualizarVisualSaldoCliente();
        }

        private async System.Threading.Tasks.Task CargarSugerenciasProductosDesdeApiAsync(string texto)
        {
            using var client = new ProductosApiClient();
            var result = await client.GetProductosAsync(busqueda: texto, limit: 20);
            if (!result.Success)
            {
                MostrarErrorProductosApi(result);
                SugerenciasListBox.ItemsSource = null;
                SugerenciasPopup.IsOpen = false;
                return;
            }

            var resultados = (result.Data ?? Array.Empty<ProductoVentaApiResponse>())
                .Select(p => p.Nombre)
                .Where(nombre => !string.IsNullOrWhiteSpace(nombre))
                .Distinct()
                .ToList();

            SugerenciasListBox.ItemsSource = resultados;
            SugerenciasPopup.IsOpen = resultados.Any();
        }

        private static void MostrarErrorProductosApi<T>(ApiRequestResult<T> result)
        {
            if (result.ErrorType is ApiErrorType.Unauthorized or ApiErrorType.SessionExpired)
            {
                return;
            }

            MessageBox.Show(result.Message);
        }

        private void ActualizarDataGrid()
        {
            ProductosDataGrid.ItemsSource = null;
            ProductosDataGrid.ItemsSource = productosEnVenta;
        }

        private void EliminarProducto_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button boton && boton.DataContext is ProductoVenta producto)
            {
                productosEnVenta.Remove(producto);
                ActualizarDataGrid();
                ActualizarTotal();
                ActualizarVisualSaldoCliente();
            }
        }

        private void LimpiarTodo_Click(object sender, RoutedEventArgs e)
        {
            productosEnVenta.Clear();
            ActualizarDataGrid();
            ActualizarTotal();
            ActualizarVisualSaldoCliente();
            BuscarProductoTextBox.Focus();

            // Reiniciar selección de cliente a Cliente General
            ClienteComboBox.SelectedIndex = 0;

            // Habilitar método de pago y seleccionar efectivo
            MetodoPagoComboBox.IsEnabled = true;
            MetodoPagoComboBox.SelectedIndex = 0;

            // Reset campos de pago
            VoucherTextBox.Clear();
            ComprobanteTextBox.Clear();
            MontoPagadoTextBox.Clear();
            VueltoTextBox.Text = "¢0.00";

            // Ocultar paneles innecesarios
            VoucherPanel.Visibility = Visibility.Collapsed;
            ComprobantePanel.Visibility = Visibility.Collapsed;
            MontoPagadoPanel.Visibility = Visibility.Visible;
            VueltoPanel.Visibility = Visibility.Visible;

            // Saldos
            SaldoClienteTextBlock.Text = "¢0.00";
            SaldoRestanteTextBlock.Text = "¢0.00";

            // Enfocar nuevamente en la búsqueda
            BuscarProductoTextBox.Clear();
            BuscarProductoTextBox.Focus();
        }


        private void ActualizarTotal()
        {
            double total = productosEnVenta.Sum(p => p.Subtotal);
            TotalVentaTextBlock.Text = $"¢{total:N2}";
            ActualizarVisualSaldoCliente();
        }

        private void Pagar_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarVenta()) return;

            string clienteSeleccionado = ClienteComboBox.SelectedItem.ToString();
            string metodoPagoSeleccionado = (MetodoPagoComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            double totalVenta = productosEnVenta.Sum(p => p.Subtotal);

            string numeroVoucher = VoucherTextBox.Text.Trim();
            string numeroComprobante = ComprobanteTextBox.Text.Trim();
            double montoPagado = 0;
            double vuelto = 0;

            // Validaciones según método de pago
            if (metodoPagoSeleccionado == "Efectivo")
            {
                if (!double.TryParse(MontoPagadoTextBox.Text, out montoPagado))
                {
                    MessageBox.Show("Por favor, ingrese un monto válido.");
                    return;
                }

                if (montoPagado < totalVenta)
                {
                    MessageBox.Show("El monto pagado no es suficiente para cubrir el total de la venta.");
                    return;
                }

                if (!double.TryParse(VueltoTextBox.Text.Replace("¢", "").Trim(), out vuelto))
                {
                    MessageBox.Show("Vuelto inválido.");
                    return;
                }
            }
            else if (metodoPagoSeleccionado == "Dólares")
            {
                if (!double.TryParse(MontoPagadoTextBox.Text, out double montoDolares))
                {
                    MessageBox.Show("Por favor, ingrese un monto válido en dólares.");
                    return;
                }

                var (_, tipoVenta) = TipoCambioHelper.ObtenerTipoCambioHoy();
                montoPagado = montoDolares * tipoVenta;
                vuelto = montoPagado - totalVenta;

                if (montoPagado < totalVenta)
                {
                    MessageBox.Show("El monto en dólares no es suficiente para cubrir el total de la venta.");
                    return;
                }
            }

            else if (metodoPagoSeleccionado == "Tarjeta")
            {
                if (string.IsNullOrWhiteSpace(numeroVoucher))
                {
                    MessageBox.Show("Debe ingresar el número de voucher para pagos con tarjeta.");
                    return;
                }
            }
            else if (metodoPagoSeleccionado == "Sinpe")
            {
                if (string.IsNullOrWhiteSpace(numeroComprobante))
                {
                    MessageBox.Show("Debe ingresar el número de SINPE para pagos por Sinpe.");
                    return;
                }
            }

            if (MessageBox.Show($"¿Desea confirmar esta venta por ¢{totalVenta:N2}?", "Confirmar Venta", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            try
            {
                using (var connection = DBConnection.GetConnection())
                {
                    if (connection.State != System.Data.ConnectionState.Open)
                        throw new Exception("La conexión no está abierta.");

                    using (var transaction = connection.BeginTransaction())
                    {
                        int idCliente = ObtenerIdCliente(connection, clienteSeleccionado);
                        int idUsuario = ObtenerIdUsuarioActual();

                        string insertVentaQuery = @"
                    INSERT INTO ventas 
                    (cliente_id, total, fecha, hora, usuario_id, metodo_pago, numero_voucher, numero_comprobante, monto_pagado, vuelto)
                    VALUES 
                    (@cliente_id, @total, @fecha, @hora, @usuario_id, @metodo_pago, @numero_voucher, @numero_comprobante, @monto_pagado, @vuelto);
                    SELECT SCOPE_IDENTITY();";

                        long facturaGenerada;
                        using (var cmdVenta = new SqlCommand(insertVentaQuery, connection, transaction))
                        {
                            cmdVenta.Parameters.AddWithValue("@cliente_id", idCliente);
                            cmdVenta.Parameters.AddWithValue("@total", totalVenta);
                            cmdVenta.Parameters.AddWithValue("@fecha", DateTime.Now.ToString("yyyy-MM-dd"));
                            cmdVenta.Parameters.AddWithValue("@hora", DateTime.Now.ToString("HH:mm:ss"));
                            cmdVenta.Parameters.AddWithValue("@usuario_id", idUsuario);
                            cmdVenta.Parameters.AddWithValue("@metodo_pago", metodoPagoSeleccionado);
                            cmdVenta.Parameters.AddWithValue("@numero_voucher", numeroVoucher);
                            cmdVenta.Parameters.AddWithValue("@numero_comprobante", numeroComprobante);
                            cmdVenta.Parameters.AddWithValue("@monto_pagado", montoPagado);
                            cmdVenta.Parameters.AddWithValue("@vuelto", vuelto);

                            object result = cmdVenta.ExecuteScalar();
                            if (result == null || result == DBNull.Value)
                                throw new Exception("No se pudo obtener el ID de la venta recién insertada.");

                            facturaGenerada = Convert.ToInt64(result);
                        }

                        foreach (var producto in productosEnVenta)
                        {
                            InsertarDetalleVenta(connection, facturaGenerada, producto, transaction);
                            ActualizarStockProducto(connection, producto, transaction);
                        }

                        if (clienteSeleccionado != "Cliente General")
                        {
                            DescontarSaldoCliente(connection, idCliente, totalVenta, transaction);
                        }

                        transaction.Commit();

                        if (metodoPagoSeleccionado == "Efectivo")
                            AbrirCajaDesdePOS58();

                        if (ImprimirReciboCheckBox != null && ImprimirReciboCheckBox.IsChecked == true)
                        {
                            var lineas = productosEnVenta.Select(p => $"{p.Nombre} x{p.Cantidad} ¢{p.Subtotal:N2}").ToList();
                            string totalTexto = $"¢{totalVenta:N2}";
                            string vueltoTexto = vuelto > 0 ? $"¢{vuelto:N2}" : "";
                            string comprobanteTexto = !string.IsNullOrWhiteSpace(numeroComprobante) ? numeroComprobante : "";
                            string fechaHora = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                            string printerName = new PrinterSettings().PrinterName;
                            string numeroFacturaTexto = facturaGenerada.ToString();
                            string nombreCajero = UserSession.NombreUsuario ?? "Desconocido";
                            string nombreCliente = clienteSeleccionado;

                            RawPrinterHelper.ImprimirReciboPOS58(
                                printerName,
                                "PULPERÍA CRA",
                                lineas,
                                totalTexto,
                                metodoPagoSeleccionado,
                                vueltoTexto,
                                comprobanteTexto,
                                fechaHora,
                                numeroFacturaTexto,
                                nombreCliente,
                                nombreCajero
                            );
                        }
                    }
                }

                MessageBox.Show("? Venta registrada correctamente.");
                ReiniciarFormularioVenta();


            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al registrar la venta: " + ex.Message);
            }
        }


        private bool ValidarVenta()
        {
            if (productosEnVenta.Count == 0)
            {
                MessageBox.Show("No hay productos para facturar.");
                return false;
            }

            if (ClienteComboBox.SelectedItem == null || MetodoPagoComboBox.SelectedItem == null)
            {
                MessageBox.Show("Debe seleccionar un cliente y un método de pago.");
                return false;
            }

            return true;
        }

        private void InsertarDetalleVenta(SqlConnection connection, long facturaGenerada, ProductoVenta producto, SqlTransaction transaction)
        {
            string insertDetalleQuery = @"
                INSERT INTO DetalleVenta (factura, producto_id, cantidad, precio_unitario)
                VALUES (@factura, @producto_id, @cantidad, @precio_unitario)";
            using var cmdDetalle = new SqlCommand(insertDetalleQuery, connection, transaction);
            cmdDetalle.Parameters.AddWithValue("@factura", facturaGenerada);
            cmdDetalle.Parameters.AddWithValue("@producto_id", producto.IdProducto);
            cmdDetalle.Parameters.AddWithValue("@cantidad", producto.Cantidad);
            cmdDetalle.Parameters.AddWithValue("@precio_unitario", Convert.ToDecimal(producto.PrecioUnitario));
            cmdDetalle.ExecuteNonQuery();
        }

        private void ActualizarStockProducto(SqlConnection connection, ProductoVenta producto, SqlTransaction transaction)
        {
            string updateStockQuery = @"
                UPDATE inventario
                SET stock = stock - @cantidad, vendido = vendido + @cantidad
                WHERE idProducto = @producto_id";
            using var cmdStock = new SqlCommand(updateStockQuery, connection, transaction);
            cmdStock.Parameters.AddWithValue("@cantidad", producto.Cantidad);
            cmdStock.Parameters.AddWithValue("@producto_id", producto.IdProducto);
            cmdStock.ExecuteNonQuery();
        }

        private void DescontarSaldoCliente(SqlConnection connection, int clienteId, double monto, SqlTransaction transaction)
        {
            string updateSaldoQuery = @"
                UPDATE cliente
                SET saldo = saldo - @monto
                WHERE idCliente = @cliente_id";

            using var command = new SqlCommand(updateSaldoQuery, connection, transaction);
            command.Parameters.AddWithValue("@monto", monto);
            command.Parameters.AddWithValue("@cliente_id", clienteId);
            command.ExecuteNonQuery();
        }

        private int ObtenerIdCliente(SqlConnection connection, string clienteSeleccionado)
        {
            if (ClienteComboBox.SelectedItem is Cliente cliente)
            {
                return cliente.idCliente;
            }

            throw new Exception("No se pudo obtener el ID del cliente.");
        }


        private int ObtenerIdUsuarioActual()
        {
            return UserSession.IdUsuario;
        }

        private void VerVentas_Click(object sender, RoutedEventArgs e)
        {
            var ventasWindow = new VentasCrudWindow();
            ventasWindow.ShowDialog(); // o .Show()
            BuscarProductoTextBox.Focus();
        }
        private void ClienteComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            double totalVenta = productosEnVenta.Sum(p => p.Subtotal);

            if (ClienteComboBox.SelectedItem is Cliente clienteSeleccionado)
            {
                if (clienteSeleccionado.nombre == "Cliente General")
                {
                    // Remover opción Saldo Cliente si existe
                    MetodoPagoComboBox.Items.Remove(saldoClienteItem);

                    MetodoPagoComboBox.IsEnabled = true;
                    MetodoPagoComboBox.SelectedIndex = 0;

                    SaldoClienteTextBlock.Text = "¢0.00";
                    SaldoRestanteTextBlock.Text = "¢0.00";

                    VoucherTextBox.Clear();
                    ComprobanteTextBox.Clear();
                    MontoPagadoTextBox.Clear();

                    MetodoPagoComboBox_SelectionChanged(null, null);
                    BuscarProductoTextBox.Focus();
                }
                else
                {
                    // Asegurar que la opción "Saldo Cliente" esté presente
                    if (!MetodoPagoComboBox.Items.Contains(saldoClienteItem))
                        MetodoPagoComboBox.Items.Add(saldoClienteItem);

                    // Forzar selección de "Saldo Cliente" y bloquear el cambio
                    MetodoPagoComboBox.SelectedItem = saldoClienteItem;
                    MetodoPagoComboBox.IsEnabled = false;

                    // Obtener saldos
                    int idCliente = clienteSeleccionado.idCliente;
                    double saldoCliente = ObtenerSaldoCliente(idCliente);
                    double saldoRestante = saldoCliente - totalVenta;

                    SaldoClienteTextBlock.Text = $"¢{saldoCliente:N2}";
                    SaldoRestanteTextBlock.Text = $"¢{saldoRestante:N2}";

                    PagarButton.IsEnabled = saldoRestante >= 0;
                    if (saldoRestante < 0)
                    {
                        MessageBox.Show("? El saldo del cliente no es suficiente para cubrir la venta.");
                    }

                    MetodoPagoComboBox_SelectionChanged(null, null);
                    BuscarProductoTextBox.Focus();
                }
            }
        }






        private double ObtenerSaldoCliente(int clienteId)
        {
            if (FeatureFlags.UseVentasClienteSelectorApi &&
                ClienteComboBox.SelectedItem is Cliente clienteSeleccionado &&
                clienteSeleccionado.idCliente == clienteId)
            {
                return clienteSeleccionado.saldo;
            }

            using var connection = DBConnection.GetConnection();
            string query = "SELECT saldo FROM cliente WHERE idCliente = @id";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", clienteId);
            object resultado = command.ExecuteScalar();

            if (resultado is decimal dec)
                return Convert.ToDouble(dec);

            return Convert.ToDouble(resultado);
        }
        private void MetodoPagoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string metodoPagoSeleccionado = (MetodoPagoComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();

            switch (metodoPagoSeleccionado)
            {
                case "Tarjeta":
                    VoucherPanel.Visibility = Visibility.Visible;
                    ComprobantePanel.Visibility = Visibility.Collapsed;
                    MontoPagadoPanel.Visibility = Visibility.Collapsed;
                    VueltoPanel.Visibility = Visibility.Collapsed;
                    ComprobanteTextBox.Clear();
                    PagarButton.IsEnabled = !string.IsNullOrWhiteSpace(VoucherTextBox.Text);
                    break;

                case "Sinpe":
                    ComprobantePanel.Visibility = Visibility.Visible;
                    VoucherPanel.Visibility = Visibility.Collapsed;
                    MontoPagadoPanel.Visibility = Visibility.Collapsed;
                    VueltoPanel.Visibility = Visibility.Collapsed;
                    VoucherTextBox.Clear();
                    PagarButton.IsEnabled = !string.IsNullOrWhiteSpace(ComprobanteTextBox.Text);
                    break;

                case "Dólares":
                    // Verificar si ya se ha ingresado el tipo de cambio hoy
                    if (!TipoCambioHelper.ExisteTipoCambioParaHoy())
                    {
                        TipoCambioWindow tipoCambioWindow = new TipoCambioWindow();
                        bool? resultado = tipoCambioWindow.ShowDialog();

                        if (resultado != true)
                        {
                            MessageBox.Show("Debe ingresar el tipo de cambio para continuar con pagos en dólares.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);

                            // Volver a efectivo
                            if (MetodoPagoComboBox.Items[0] is ComboBoxItem efectivoItem)
                                MetodoPagoComboBox.SelectedItem = efectivoItem;

                            return;
                        }
                    }

                    VoucherPanel.Visibility = Visibility.Collapsed;
                    ComprobantePanel.Visibility = Visibility.Collapsed;
                    MontoPagadoPanel.Visibility = Visibility.Visible;
                    VueltoPanel.Visibility = Visibility.Visible;
                    PagarButton.IsEnabled = true;
                    break;

                default: // Efectivo o Saldo Cliente
                    VoucherPanel.Visibility = Visibility.Collapsed;
                    ComprobantePanel.Visibility = Visibility.Collapsed;
                    MontoPagadoPanel.Visibility = Visibility.Visible;
                    VueltoPanel.Visibility = Visibility.Visible;
                    VoucherTextBox.Clear();
                    ComprobanteTextBox.Clear();
                    PagarButton.IsEnabled = true;
                    break;
            }

            BuscarProductoTextBox.Focus();
        }




        private void CalcularVuelto(object sender, TextChangedEventArgs e)
        {
            string metodoPagoSeleccionado = (MetodoPagoComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            double total = productosEnVenta.Sum(p => p.Subtotal);

            if (metodoPagoSeleccionado == "Dólares")
            {
                if (double.TryParse(MontoPagadoTextBox.Text, out double montoDolares))
                {
                    if (TipoCambioHelper.ObtenerTipoCambioHoy() is (double compra, double venta))
                    {
                        double pagadoColones = montoDolares * venta;
                        double vuelto = pagadoColones - total;

                        VueltoTextBox.Text = vuelto < 0 ? "¢0.00" : $"¢{vuelto:N2}";
                        PagarButton.IsEnabled = vuelto >= 0;
                    }
                    else
                    {
                        VueltoTextBox.Text = "¢0.00";
                        PagarButton.IsEnabled = false;
                    }
                }
                else
                {
                    VueltoTextBox.Text = "¢0.00";
                    PagarButton.IsEnabled = false;
                }
            }
            else
            {
                if (double.TryParse(MontoPagadoTextBox.Text, out double pagado))
                {
                    double vuelto = pagado - total;
                    VueltoTextBox.Text = vuelto < 0 ? "¢0.00" : $"¢{vuelto:N2}";
                    PagarButton.IsEnabled = vuelto >= 0;
                }
                else
                {
                    VueltoTextBox.Text = "¢0.00";
                    PagarButton.IsEnabled = false;
                }
            }
        }

        private void VoucherTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if ((MetodoPagoComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() == "Tarjeta")
            {
                PagarButton.IsEnabled = !string.IsNullOrWhiteSpace(VoucherTextBox.Text);
            }
        }

        private void ComprobanteTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if ((MetodoPagoComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() == "Sinpe")
            {
                PagarButton.IsEnabled = !string.IsNullOrWhiteSpace(ComprobanteTextBox.Text);
            }
        }


        private void ActualizarSaldoCliente(int clienteId, double nuevoSaldo)
        {
            using var connection = DBConnection.GetConnection();
            string query = "UPDATE cliente SET saldo = @nuevoSaldo WHERE idCliente = @id";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@nuevoSaldo", nuevoSaldo);
            command.Parameters.AddWithValue("@id", clienteId);
            command.ExecuteNonQuery();
        }

        private void ActualizarVisualSaldoCliente()
        {
            if (ClienteComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                int idCliente = Convert.ToInt32(item.Tag);
                double saldoCliente = ObtenerSaldoCliente(idCliente);
                double totalVenta = productosEnVenta.Sum(p => p.Subtotal);
                double saldoRestante = saldoCliente - totalVenta;

                // Mostrar valores
                SaldoClienteTextBlock.Text = $"¢{saldoCliente:N2}";
                SaldoRestanteTextBlock.Text = $"¢{saldoRestante:N2}";

                // Habilitar o deshabilitar botón Pagar
                PagarButton.IsEnabled = saldoRestante >= 0;
            }
        }

        
        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Cierra la ventana actual
        }
        private void ProductosDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Column.Header.ToString() == "Cantidad")
            {
                var producto = e.Row.Item as ProductoVenta;
                if (producto == null) return;

                if (e.EditingElement is TextBox textBox)
                {
                    if (!int.TryParse(textBox.Text, out int nuevaCantidad) || nuevaCantidad <= 0)
                    {
                        MessageBox.Show("La cantidad debe ser un número mayor que cero.");
                        textBox.Text = producto.Cantidad.ToString();
                        return;
                    }

                    if (nuevaCantidad > producto.StockDisponible)
                    {
                        MessageBox.Show($"No hay suficiente stock disponible. Máximo permitido: {producto.StockDisponible}");
                        textBox.Text = producto.StockDisponible.ToString();
                        return;
                    }

                    producto.Cantidad = nuevaCantidad;
                }
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                ActualizarTotal();
                ActualizarVisualSaldoCliente();
            }));
        }
        private void AbrirCajaDesdePOS58()
        {
            byte[] abrirCaja = new byte[] { 0x1B, 0x70, 0x00, 0x3C, 0x78 }; // Comando estándar ESC/POS
            bool ok = RawPrinterHelper.SendBytesToPrinter("POS-58", abrirCaja);
            if (!ok)
            {
                MessageBox.Show("No se pudo abrir la caja registradora.");
            }
        }

        private void ImprimirReciboCheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }
        private void ReiniciarFormularioVenta()
        {
            // Limpiar productos
            productosEnVenta.Clear();
            ActualizarDataGrid();
            ActualizarTotal();

            // Cliente General
            ClienteComboBox.SelectedIndex = 0;

            // Método de Pago en Efectivo
            MetodoPagoComboBox.SelectedIndex = 0;
            MetodoPagoComboBox.IsEnabled = true;

            // Limpiar campos de pago
            VoucherTextBox.Text = "";
            ComprobanteTextBox.Text = "";
            MontoPagadoTextBox.Text = "";
            VueltoTextBox.Text = "¢0.00";

            // Mostrar solo campos necesarios para efectivo
            VoucherPanel.Visibility = Visibility.Collapsed;
            ComprobantePanel.Visibility = Visibility.Collapsed;
            MontoPagadoPanel.Visibility = Visibility.Visible;
            VueltoPanel.Visibility = Visibility.Visible;

            // Reset saldos
            SaldoClienteTextBlock.Text = "¢0.00";
            SaldoRestanteTextBlock.Text = "¢0.00";

            // Desactivar el checkbox de impresión
            ImprimirReciboCheckBox.IsChecked = false;

            // Botón de pagar activo
            PagarButton.IsEnabled = true;

            // Foco en el campo de búsqueda
            BuscarProductoTextBox.Clear();
            BuscarProductoTextBox.Focus();
        }

        private async void BuscarProductoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string texto = BuscarProductoTextBox.Text.Trim();
            if (texto.Length < 2)
            {
                SugerenciasPopup.IsOpen = false;
                return;
            }

            if (FeatureFlags.UseVentasProductosApi)
            {
                await CargarSugerenciasProductosDesdeApiAsync(texto);
                return;
            }

            try
            {
                using var connection = DBConnection.GetConnection();
                string query = "SELECT nombre FROM inventario WHERE nombre LIKE @nombre + '%'";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@nombre", texto);

                var resultados = new List<string>();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    resultados.Add(reader.GetString(0));
                }

                SugerenciasListBox.ItemsSource = resultados;
                SugerenciasPopup.IsOpen = resultados.Any();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al buscar sugerencias: " + ex.Message);
            }
        }
        private void SugerenciasListBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (SugerenciasListBox.SelectedItem is string seleccionado)
            {
                BuscarProductoTextBox.Text = seleccionado;
                SugerenciasPopup.IsOpen = false;
                AgregarProductoPorCodigo();
            }
        }
        private void SugerenciasListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && SugerenciasListBox.SelectedItem is string seleccionado)
            {
                BuscarProductoTextBox.Text = seleccionado;
                SugerenciasPopup.IsOpen = false;
                AgregarProductoPorCodigo();
            }
        }
        private async void ClienteComboBox_KeyUp(object sender, KeyEventArgs e)
        {
            string texto = ClienteComboBox.Text.ToLower().Trim();

            if (FeatureFlags.UseVentasClienteSelectorApi)
            {
                await CargarClientesDesdeApiAsync(string.IsNullOrWhiteSpace(texto) ? null : texto);
                ClienteComboBox.IsDropDownOpen = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(texto))
            {
                ClientesFiltrados = new List<Cliente>(todosLosClientes);
            }
            else
            {
                ClientesFiltrados = todosLosClientes
                    .Where(c => c.nombre.ToLower().Contains(texto))
                    .ToList();
            }

            ClienteComboBox.ItemsSource = ClientesFiltrados;
            ClienteComboBox.IsDropDownOpen = true;
        }



        private void ClienteComboBox_DropDownOpened(object sender, EventArgs e)
        {
            ClienteComboBox.ItemsSource = ClientesFiltrados;
        }
        private void ClienteComboBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ClienteComboBox.IsEditable && !ClienteComboBox.IsDropDownOpen)
            {
                ClienteComboBox.IsDropDownOpen = true;

                if (limpiarTextoCliente)
                {
                    ClienteComboBox.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ClienteComboBox.Text = "";
                        limpiarTextoCliente = false;
                    }), System.Windows.Threading.DispatcherPriority.Input);
                }
            }
        }


        private void ClienteComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (ClienteComboBox.SelectedItem is not Cliente)
            {
                // Asignar Cliente General
                var clienteGeneral = todosLosClientes.FirstOrDefault(c => c.nombre == "Cliente General");
                if (clienteGeneral != null)
                {
                    ClienteComboBox.SelectedItem = clienteGeneral;
                }
            }
        }









        private void ImprimirReciboCheckBox_Checked_1(object sender, RoutedEventArgs e)
        {

        }


       


    }

}
