using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PulperiaPOS.DataAccess;
using PulperiaPOS.Views;

namespace PulperiaPOS
{
    public static class TipoCambioHelper
    {
        public static bool ExisteTipoCambioParaHoy()
        {
            using var conn = DBConnection.GetConnection();
            string query = "SELECT COUNT(*) FROM TipoCambioDolar WHERE fecha = @fecha";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@fecha", DateTime.Today);
            return (int)cmd.ExecuteScalar() > 0;
        }

        public static void GuardarTipoCambio(DateTime fecha, double compra, double venta)
        {
            using var conn = DBConnection.GetConnection();

            // Si ya existe, actualiza
            string query = @"
            IF EXISTS (SELECT 1 FROM TipoCambioDolar WHERE fecha = @fecha)
                UPDATE TipoCambioDolar SET compra = @compra, venta = @venta WHERE fecha = @fecha
            ELSE
                INSERT INTO TipoCambioDolar (fecha, compra, venta) VALUES (@fecha, @compra, @venta)";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@fecha", fecha);
            cmd.Parameters.AddWithValue("@compra", compra);
            cmd.Parameters.AddWithValue("@venta", venta);
            cmd.ExecuteNonQuery();
        }

        public static (double compra, double venta) ObtenerTipoCambioHoy()
        {
            using var conn = DBConnection.GetConnection();
            string query = "SELECT compra, venta FROM TipoCambioDolar WHERE fecha = @fecha";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@fecha", DateTime.Today);
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                double compra = Convert.ToDouble(reader["compra"]);
                double venta = Convert.ToDouble(reader["venta"]);
                return (compra, venta);
            }

            throw new Exception("No se ha configurado el tipo de cambio para hoy.");
        }
    }
}

