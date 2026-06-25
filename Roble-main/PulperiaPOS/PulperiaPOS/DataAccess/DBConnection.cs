using System;
using System.Data.SqlClient;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace PulperiaPOS.DataAccess
{
    public static class DBConnection
    {
        private const string ConnectionStringName = "PosDatabase";
        private const string EnvironmentVariableName = "POS_DATABASE_CONNECTION_STRING";
        private static readonly Lazy<string> connectionString = new(LoadConnectionString);

        public static SqlConnection GetConnection()
        {
            try
            {
                var connection = new SqlConnection(connectionString.Value);
                connection.Open();
                return connection;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteSafeLog(ex);
                throw new InvalidOperationException(
                    "No se pudo abrir la conexion a la base de datos. Revise la configuracion local de ConnectionStrings:PosDatabase.",
                    ex);
            }
        }

        public static string GetConnectionString() => connectionString.Value;

        private static string LoadConnectionString()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
                .Build();

            var configuredConnectionString = configuration.GetConnectionString(ConnectionStringName);
            var environmentConnectionString = Environment.GetEnvironmentVariable(EnvironmentVariableName);
            var resolvedConnectionString = !string.IsNullOrWhiteSpace(environmentConnectionString)
                ? environmentConnectionString
                : configuredConnectionString;

            if (string.IsNullOrWhiteSpace(resolvedConnectionString) ||
                resolvedConnectionString.Contains("CAMBIAR_ESTE_VALOR", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "No existe una cadena de conexion local valida para ConnectionStrings:PosDatabase. Configure appsettings.Development.json o la variable POS_DATABASE_CONNECTION_STRING.");
            }

            return resolvedConnectionString;
        }

        private static void WriteSafeLog(Exception ex)
        {
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db_log.txt");
                File.AppendAllText(logPath, $"[{DateTime.Now}] Error al abrir conexion: {ex.GetType().Name}{Environment.NewLine}");
            }
            catch
            {
                // No bloquear el flujo si no se puede escribir el log local.
            }
        }
    }
}
