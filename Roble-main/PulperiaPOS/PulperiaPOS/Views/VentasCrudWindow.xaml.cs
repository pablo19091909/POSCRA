using PulperiaPOS.DataAccess;
using PulperiaPOS.ApiClients;
using PulperiaPOS.Configuration;
using PulperiaPOS.Models.Ventas;
using PulperiaPOS.Views;
using System;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics; // Al inicio del archivo
using System.Drawing.Printing;
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

namespace PulperiaPOS.Views
{
    public partial class VentasCrudWindow : Window
    {
        public VentasCrudWindow()
        {
            InitializeComponent();
            CargarVentas();
        }

        private void CargarVentas()
        {
            try
            {
                using (var connection = DBConnection.GetConnection())
                {
                    string query = @"
                        SELECT 
                            v.factura,
                            COALESCE(c.nombre, 'Venta sin cliente') AS nombre_cliente,
                            v.total,
                            v.fecha,
                            v.hora,
                            v.metodo_pago,
                            v.numero_voucher,
                            v.numero_comprobante,
                            v.monto_pagado,
                            v.vuelto,
                            v.cliente_id,
                            v.usuario_id,
                            CASE WHEN vp.idPago IS NOT NULL AND mc.idMovimiento IS NOT NULL THEN 1 ELSE 0 END AS es_api_efectivo,
                            CASE WHEN vr.idReversa IS NOT NULL THEN 1 ELSE 0 END AS reversada_api
                        FROM ventas v
                        LEFT JOIN cliente c ON v.cliente_id = c.idCliente
                        OUTER APPLY (
                            SELECT TOP (1) idPago
                            FROM venta_pago
                            WHERE factura = v.factura
                              AND metodo_pago = 'Efectivo'
                              AND estado = 'Registrado'
                            ORDER BY idPago DESC
                        ) vp
                        OUTER APPLY (
                            SELECT TOP (1) idMovimiento
                            FROM movimiento_caja
                            WHERE factura = v.factura
                              AND pago_id = vp.idPago
                              AND tipo_movimiento = 'VentaEfectivo'
                              AND estado = 'Confirmado'
                            ORDER BY idMovimiento DESC
                        ) mc
                        OUTER APPLY (
                            SELECT TOP (1) idReversa
                            FROM venta_reversa
                            WHERE factura = v.factura
                              AND estado = 'Confirmada'
                            ORDER BY idReversa DESC
                        ) vr";

                    var adapter = new SqlDataAdapter(query, connection);
                    var dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    VentasDataGrid.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando ventas: " + ex.Message);
            }
        }

        private void RefrescarVentas_Click(object sender, RoutedEventArgs e)
        {
            CargarVentas();
        }

        private void AgregarVenta_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var connection = DBConnection.GetConnection())
                {
                    string fecha = DateTime.Now.ToString("yyyy-MM-dd");
                    string hora = DateTime.Now.ToString("HH:mm:ss");

                    string query = @"
                        INSERT INTO ventas (cliente_id, total, fecha, hora, usuario_id, metodo_pago, numero_voucher, numero_comprobante, monto_pagado, vuelto)
                        VALUES (NULL, 0, @fecha, @hora, 1, 'Efectivo', '', '', 0, 0)";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@fecha", fecha);
                        command.Parameters.AddWithValue("@hora", hora);
                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Venta agregada correctamente.");
                CargarVentas();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al agregar venta: " + ex.Message);
            }
        }


        private void ActualizarVenta_Click(object sender, RoutedEventArgs e)
        {
            if (VentasDataGrid.SelectedItem == null)
            {
                MessageBox.Show("Seleccione una venta para actualizar.");
                return;
            }

            DataRowView row = (DataRowView)VentasDataGrid.SelectedItem;

            try
            {
                using (var connection = DBConnection.GetConnection())
                {
                    string query = "UPDATE ventas SET total = total + 100 WHERE factura = @factura";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@factura", row["factura"]);
                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Venta actualizada correctamente.");
                CargarVentas();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al actualizar venta: " + ex.Message);
            }
        }


        private void EliminarVenta_Click(object sender, RoutedEventArgs e)
        {
            if (VentasDataGrid.SelectedItem == null)
            {
                MessageBox.Show("Seleccione una venta para eliminar.");
                return;
            }

            if (VentasDataGrid.SelectedItem is not DataRowView ventaSeleccionada)
            {
                MessageBox.Show("Error al obtener la venta seleccionada.");
                return;
            }

            if (!ventaSeleccionada.Row.Table.Columns.Contains("factura") || ventaSeleccionada["factura"] == DBNull.Value)
            {
                MessageBox.Show("La factura seleccionada no es válida o no existe.");
                return;
            }

            long numeroFactura = Convert.ToInt64(ventaSeleccionada["factura"]);

            if (FeatureFlags.UseVentasApiReversaWrite && EsVentaApiEfectivo(ventaSeleccionada))
            {
                MessageBox.Show("Esta venta API en efectivo no puede borrarse físicamente. Use Modo Reversa API.", "Modo Reversa API", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var connection = DBConnection.GetConnection();
                using var transaction = connection.BeginTransaction();
                int idCliente = 0;
                float totalVenta = 0;

                string obtenerVenta = "SELECT cliente_id, total FROM ventas WHERE factura = @factura";
                using (var cmd = new SqlCommand(obtenerVenta, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@factura", numeroFactura);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            throw new Exception("No se encontró la venta.");

                        idCliente = reader["cliente_id"] != DBNull.Value ? Convert.ToInt32(reader["cliente_id"]) : 0;
                        totalVenta = reader["total"] != DBNull.Value ? Convert.ToSingle(reader["total"]) : 0;
                    }
                }

                string obtenerDetalle = "SELECT producto_id, cantidad FROM DetalleVenta WHERE factura = @factura";
                var detalles = new List<(string idProducto, int cantidad)>();

                using (var cmd = new SqlCommand(obtenerDetalle, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@factura", numeroFactura);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string idProducto = reader["producto_id"].ToString();
                            int cantidad = reader["cantidad"] != DBNull.Value ? Convert.ToInt32(reader["cantidad"]) : 0;

                            if (!string.IsNullOrWhiteSpace(idProducto) && cantidad > 0)
                                detalles.Add((idProducto, cantidad));
                        }
                    }
                }

                foreach (var (idProducto, cantidad) in detalles)
                {
                    using var updateCmd = new SqlCommand(@"
                        UPDATE inventario 
                        SET stock = stock + @cantidad, 
                            vendido = CASE WHEN vendido - @cantidad < 0 THEN 0 ELSE vendido - @cantidad END
                        WHERE idProducto = @idProducto", connection, transaction);

                    updateCmd.Parameters.AddWithValue("@cantidad", cantidad);
                    updateCmd.Parameters.AddWithValue("@idProducto", idProducto);
                    updateCmd.ExecuteNonQuery();
                }

                using (var cmd = new SqlCommand("DELETE FROM DetalleVenta WHERE factura = @factura", connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@factura", numeroFactura);
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new SqlCommand("DELETE FROM ventas WHERE factura = @factura", connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@factura", numeroFactura);
                    cmd.ExecuteNonQuery();
                }

                if (idCliente > 0)
                {
                    using var cmd = new SqlCommand("UPDATE cliente SET saldo = saldo + @monto WHERE idCliente = @idCliente", connection, transaction);
                    cmd.Parameters.AddWithValue("@monto", totalVenta);
                    cmd.Parameters.AddWithValue("@idCliente", idCliente);
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();

                MessageBox.Show("✅ Venta eliminada correctamente.");
                CargarVentas();
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Error al eliminar la venta:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ReversarVentaApi_Click(object sender, RoutedEventArgs e)
        {
            if (!FeatureFlags.UseVentasApiReversaWrite)
            {
                MessageBox.Show("Modo Reversa API está deshabilitado.", "Modo Reversa API", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (sender is not Button button || button.Tag is not DataRowView row)
            {
                MessageBox.Show("Seleccione una venta válida.", "Modo Reversa API", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!UserSession.HasPermission("Ventas.Reversar"))
            {
                MessageBox.Show("No tiene permiso para reversar ventas por API.", "Modo Reversa API", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!EsVentaApiEfectivo(row))
            {
                MessageBox.Show("Solo ventas API pagadas completamente en efectivo pueden reversarse en este modo.", "Modo Reversa API", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EstaReversadaApi(row))
            {
                MessageBox.Show("Esta venta ya fue reversada por API.", "Modo Reversa API", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var factura = Convert.ToInt64(row["factura"]);
            var total = Convert.ToDecimal(row["total"]);
            var metodoPago = Convert.ToString(row["metodo_pago"]) ?? "Efectivo";

            var dialog = new ReversaVentaApiWindow(factura, total, metodoPago)
            {
                Owner = this
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            button.IsEnabled = false;
            VentasDataGrid.IsEnabled = false;

            try
            {
                using var client = new VentasApiClient();
                var request = new ReversarVentaRequest
                {
                    IdempotencyKey = Guid.NewGuid(),
                    Motivo = dialog.Motivo
                };

                var result = await client.ReversarVentaAsync(Convert.ToInt32(factura), request);
                if (!result.Success)
                {
                    MessageBox.Show(result.Message, "Modo Reversa API", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBox.Show("La venta fue reversada correctamente por API.", "Modo Reversa API", MessageBoxButton.OK, MessageBoxImage.Information);
                CargarVentas();
            }
            finally
            {
                VentasDataGrid.IsEnabled = true;
                button.IsEnabled = true;
            }
        }

        private static bool EsVentaApiEfectivo(DataRowView row)
        {
            return row.Row.Table.Columns.Contains("es_api_efectivo") &&
                   row["es_api_efectivo"] != DBNull.Value &&
                   Convert.ToInt32(row["es_api_efectivo"]) == 1;
        }

        private static bool EstaReversadaApi(DataRowView row)
        {
            return row.Row.Table.Columns.Contains("reversada_api") &&
                   row["reversada_api"] != DBNull.Value &&
                   Convert.ToInt32(row["reversada_api"]) == 1;
        }

        private void VerDetalleVenta_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag != null && int.TryParse(button.Tag.ToString(), out int ventaId))
            {
                var detalleWindow = new DetalleVentaWindow(ventaId);
                detalleWindow.ShowDialog();
            }
        }

        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Cierra la ventana actual
        }
        private void BtnReimprimir_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button boton && boton.DataContext is DataRowView row)
            {
                try
                {
                    var venta = new Venta
                    {
                        factura = Convert.ToInt64(row["factura"]),
                        nombre_cliente = row["nombre_cliente"].ToString(),
                        cliente_id = row["cliente_id"] != DBNull.Value ? Convert.ToInt32(row["cliente_id"]) : 0,
                        total = Convert.ToSingle(row["total"]),
                        fecha = row["fecha"].ToString(),
                        hora = row["hora"].ToString(),
                        usuario_id = row["usuario_id"] != DBNull.Value ? Convert.ToInt32(row["usuario_id"]) : 0,
                        metodo_pago = row["metodo_pago"].ToString(),
                        numero_voucher = row["numero_voucher"].ToString(),
                        numero_comprobante = row["numero_comprobante"].ToString(),
                        monto_pagado = Convert.ToSingle(row["monto_pagado"]),
                        vuelto = Convert.ToSingle(row["vuelto"])
                    };

                    var lineas = new List<string>();
                    using (var conn = DBConnection.GetConnection())
                    {
                        string query = @"SELECT d.producto_id, i.nombre, d.cantidad, d.precio_unitario
                                 FROM DetalleVenta d
                                 JOIN inventario i ON i.idProducto = d.producto_id
                                 WHERE d.factura = @factura";

                        using var cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@factura", venta.factura);
                        using var reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            string nombre = reader["nombre"].ToString();
                            int cantidad = Convert.ToInt32(reader["cantidad"]);
                            float precio = Convert.ToSingle(reader["precio_unitario"]);
                            lineas.Add($"{nombre} x{cantidad} ₡{(cantidad * precio):N2}");
                        }
                    }

                    string totalTexto = $"₡{venta.total:N2}";
                    string vueltoTexto = venta.vuelto > 0 ? $"₡{venta.vuelto:N2}" : "";
                    string comprobanteTexto = !string.IsNullOrWhiteSpace(venta.numero_comprobante) ? venta.numero_comprobante : "";
                    string fechaHora = $"{venta.fecha} {venta.hora}";
                    string printerName = new PrinterSettings().PrinterName;
                    string numeroFacturaTexto = venta.factura.ToString();
                    string nombreCliente = string.IsNullOrWhiteSpace(venta.nombre_cliente) ? "Cliente General" : venta.nombre_cliente;
                    string nombreCajero = UserSession.NombreUsuario ?? "Desconocido";

                    RawPrinterHelper.ImprimirReciboPOS58(
                        printerName,
                        "PULPERÍA CRA",
                        lineas,
                        totalTexto,
                        venta.metodo_pago,
                        vueltoTexto,
                        comprobanteTexto,
                        fechaHora,
                        numeroFacturaTexto,
                        nombreCliente,
                        nombreCajero
                    );

                    MessageBox.Show("Factura reimpresa correctamente.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al reimprimir: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("No se pudo obtener la información de la venta.");
            }
        }

        private List<ProductoVenta> ObtenerProductosDeVenta(long facturaId)
        {
            var productos = new List<ProductoVenta>();
            using var conn = DBConnection.GetConnection();
            string query = @"SELECT i.nombre, dv.cantidad, dv.precio_unitario
                            FROM DetalleVenta dv
                            JOIN inventario i ON i.idProducto = dv.producto_id
                            WHERE dv.factura = @factura";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@factura", facturaId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                productos.Add(new ProductoVenta
                {
                    Nombre = reader.GetString(0),
                    Cantidad = reader.GetInt32(1),
                    PrecioUnitario = Convert.ToSingle(reader.GetDouble(2))
                });
            }

            return productos;
        }


        public class Venta
        {
            public long factura { get; set; }
            public string nombre_cliente { get; set; }
            public int cliente_id { get; set; }
            public float total { get; set; }
            public string fecha { get; set; }
            public string hora { get; set; }
            public int usuario_id { get; set; }
            public string metodo_pago { get; set; }
            public string numero_voucher { get; set; }
            public string numero_comprobante { get; set; }
            public float monto_pagado { get; set; }
            public float vuelto { get; set; }
        }
        public class ProductoVenta
        {
            public string Nombre { get; set; }
            public int Cantidad { get; set; }
            public float PrecioUnitario { get; set; }
            public float Subtotal => Cantidad * PrecioUnitario;
        }
    }
}


