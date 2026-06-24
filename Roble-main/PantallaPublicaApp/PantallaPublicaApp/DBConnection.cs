using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data.SqlClient;
using System.Threading.Tasks;
    using System.IO;


namespace PantallaPublicaApp
{
    public static class DBConnection
    {
        private static readonly string connectionString = "Server=tcp:pulperiasqlserver.database.windows.net,1433;" +
                                                           "Initial Catalog=roblealtodb;" +
                                                           "Persist Security Info=False;" +
                                                           "User ID=CRAPOS1948;" +
                                                           "Password=2025@POS_CRA__#;" + // Cámbiala por seguridad más adelante
                                                           "MultipleActiveResultSets=False;" +
                                                           "Encrypt=True;" +
                                                           "TrustServerCertificate=False;" +
                                                           "Connection Timeout=60;";

        public static SqlConnection GetConnection()
        {
            try
            {
                var connection = new SqlConnection(connectionString);
                connection.Open();
                return connection;
            }
            catch (Exception ex)
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db_log.txt");
                File.AppendAllText(logPath, $"[{DateTime.Now}] Error al abrir conexión: {ex.Message}{Environment.NewLine}");
                throw;
            }
        }

        public static string GetConnectionString() => connectionString;
    }
}
