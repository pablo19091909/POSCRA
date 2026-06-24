using System;
using System.Data.SqlClient;
using System.Windows;

namespace PulperiaPOS.DataAccess
{
    public static class AzureConnectionTester
    {
        public static void ProbarConexion()
        {
            try
            {
                using (var conn = DBConnection.GetConnection())
                {
                    // Ya viene abierta desde DBConnection
                    if (conn.State == System.Data.ConnectionState.Open)
                    {
                        MessageBox.Show("✅ Conexión a Azure SQL exitosa.", "Conexión OK", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("❌ Conexión fallida. El estado no es Open.", "Estado incorrecto", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                MessageBox.Show($"❌ Error SQL al conectar:\n{sqlEx.Message}", "Error de conexión SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error inesperado:\n{ex.Message}", "Error General", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
