using System;
using System.Data.SqlClient;
using System.Windows;
using PulperiaPOS.DataAccess;

namespace PulperiaPOS
{
    public static class CajaHelper
    {
        public class TotalesCaja
        {
            public double Ventas { get; set; }
            public double Ingresos { get; set; }
            public double Retiros { get; set; }
            public double Cierres { get; set; }
            public double SinpeClientes { get; set; }

            public double TotalDisponible => Ventas + Ingresos - Retiros;
        }

        public static DateTime? ObtenerUltimaHoraDeCierreHoy()
        {
            try
            {
                using (var connection = DBConnection.GetConnection())
                {
                    string fecha = DateTime.Now.ToString("yyyy-MM-dd");
                    using (var cmd = new SqlCommand("SELECT TOP 1 fecha, hora FROM cierre_caja WHERE fecha = @fecha ORDER BY hora DESC", connection))
                    {
                        cmd.Parameters.AddWithValue("@fecha", fecha);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                DateTime fechaCierre = Convert.ToDateTime(reader["fecha"]);
                                TimeSpan horaCierre = TimeSpan.Parse(reader["hora"].ToString());
                                return fechaCierre.Date + horaCierre;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al obtener último cierre: " + ex.Message);
            }

            return null;
        }

        public static TotalesCaja ObtenerTotalesCaja()
        {
            var totales = new TotalesCaja();
            string fechaHoy = DateTime.Now.ToString("yyyy-MM-dd");
            DateTime? ultimaHoraCierre = ObtenerUltimaHoraDeCierreHoy();

            try
            {
                using (var connection = DBConnection.GetConnection())
                {
                    string filtroFecha = "CONVERT(DATE, fecha) = @fecha";
                    string filtroHora = ultimaHoraCierre.HasValue ? "AND hora > @hora" : "";

                    // Ventas en efectivo desde el último cierre
                    using (var cmd = new SqlCommand($@"
                        SELECT SUM(total) 
                        FROM ventas 
                        WHERE {filtroFecha} AND LOWER(metodo_pago) = 'efectivo' {filtroHora}", connection))
                    {
                        cmd.Parameters.AddWithValue("@fecha", fechaHoy);
                        if (ultimaHoraCierre.HasValue)
                            cmd.Parameters.AddWithValue("@hora", ultimaHoraCierre.Value.ToString("HH:mm:ss"));

                        var result = cmd.ExecuteScalar();
                        totales.Ventas = result != DBNull.Value ? Convert.ToDouble(result) : 0;
                    }

                    // Ingresos desde el último cierre
                    using (var cmd = new SqlCommand($@"
                        SELECT SUM(monto) 
                        FROM ingreso_caja 
                        WHERE {filtroFecha} {filtroHora}", connection))
                    {
                        cmd.Parameters.AddWithValue("@fecha", fechaHoy);
                        if (ultimaHoraCierre.HasValue)
                            cmd.Parameters.AddWithValue("@hora", ultimaHoraCierre.Value.ToString("HH:mm:ss"));

                        var result = cmd.ExecuteScalar();
                        totales.Ingresos = result != DBNull.Value ? Convert.ToDouble(result) : 0;
                    }

                    // Retiros desde el último cierre
                    using (var cmd = new SqlCommand($@"
                        SELECT SUM(monto) 
                        FROM retiro_caja 
                        WHERE {filtroFecha} {filtroHora}", connection))
                    {
                        cmd.Parameters.AddWithValue("@fecha", fechaHoy);
                        if (ultimaHoraCierre.HasValue)
                            cmd.Parameters.AddWithValue("@hora", ultimaHoraCierre.Value.ToString("HH:mm:ss"));

                        var result = cmd.ExecuteScalar();
                        totales.Retiros = result != DBNull.Value ? Convert.ToDouble(result) : 0;
                    }

                    // SINPE clientes
                    using (var cmd = new SqlCommand("SELECT SUM(saldo) FROM cliente WHERE comprobante IS NOT NULL AND CONVERT(DATE, fecha_carga_saldo) = @fecha", connection))
                    {
                        cmd.Parameters.AddWithValue("@fecha", fechaHoy);
                        var result = cmd.ExecuteScalar();
                        totales.SinpeClientes = result != DBNull.Value ? Convert.ToDouble(result) : 0;
                    }
                }

                return totales;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al obtener totales de caja: " + ex.Message);
                return totales;
            }
        }
        public static double ObtenerDineroAcumuladoCajaChica()
        {
            double totalVentasEfectivo = 0;
            double totalIngresos = 0;
            double totalRetiros = 0;

            try
            {
                using (var connection = DBConnection.GetConnection())
                {
                    // Todas las ventas en efectivo (sin filtro por fecha)
                    using (var cmd = new SqlCommand("SELECT SUM(total) FROM ventas WHERE LOWER(metodo_pago) = 'efectivo'", connection))
                    {
                        var result = cmd.ExecuteScalar();
                        totalVentasEfectivo = result != DBNull.Value ? Convert.ToDouble(result) : 0;
                    }

                    // Todos los ingresos manuales
                    using (var cmd = new SqlCommand("SELECT SUM(monto) FROM ingreso_caja", connection))
                    {
                        var result = cmd.ExecuteScalar();
                        totalIngresos = result != DBNull.Value ? Convert.ToDouble(result) : 0;
                    }

                    // Todos los retiros
                    using (var cmd = new SqlCommand("SELECT SUM(monto) FROM retiro_caja", connection))
                    {
                        var result = cmd.ExecuteScalar();
                        totalRetiros = result != DBNull.Value ? Convert.ToDouble(result) : 0;
                    }
                }

                // El total acumulado en caja chica es ventas + ingresos - retiros
                return totalVentasEfectivo + totalIngresos - totalRetiros;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al calcular el total acumulado en caja: " + ex.Message);
                return 0;
            }
        }

    }
}
