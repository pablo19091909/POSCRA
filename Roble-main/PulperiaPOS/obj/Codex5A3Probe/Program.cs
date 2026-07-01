using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;

static string? ReadConnectionString()
{
    var env = Environment.GetEnvironmentVariable("POS_API_DATABASE_CONNECTION_STRING");
    if (!string.IsNullOrWhiteSpace(env)) return env;
    foreach (var path in new[] { @"POS.Api\appsettings.Development.json", @"POS.Api\appsettings.json" })
    {
        if (!File.Exists(path)) continue;
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        if (doc.RootElement.TryGetProperty("ConnectionStrings", out var cs) &&
            cs.TryGetProperty("PosDatabase", out var pos) &&
            !string.IsNullOrWhiteSpace(pos.GetString())) return pos.GetString();
    }
    return null;
}

var connectionString = ReadConnectionString();
if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("Conexion no configurada para POS.Api.");
await using var conn = new SqlConnection(connectionString);
await conn.OpenAsync();
await using var cmd = conn.CreateCommand();
cmd.CommandText = @"
SET NOCOUNT ON;
SELECT
  environment_ok = CONVERT(int, CASE WHEN EXISTS (SELECT 1 FROM dbo.app_environment WHERE id=1 AND environment_name=N'Test' AND writes_allowed_for_testing=1) THEN 1 ELSE 0 END),
  open_turns = (SELECT COUNT_BIG(1) FROM dbo.caja_turno WHERE caja_codigo=N'CAJA_PRINCIPAL_TEST' AND estado=N'Abierto'),
  closing_turns = (SELECT COUNT_BIG(1) FROM dbo.caja_turno WHERE caja_codigo=N'CAJA_PRINCIPAL_TEST' AND estado=N'EnCierre'),
  pending_caja_idem = (SELECT COUNT_BIG(1) FROM dbo.caja_idempotencia WHERE estado IN (N'EnProceso', N'Fallida')),
  pending_venta_idem = (SELECT COUNT_BIG(1) FROM dbo.venta_idempotencia WHERE estado IN (N'EnProceso', N'Fallida')),
  ventas = (SELECT COUNT_BIG(1) FROM dbo.ventas),
  detalle = (SELECT COUNT_BIG(1) FROM dbo.DetalleVenta),
  pagos = (SELECT COUNT_BIG(1) FROM dbo.venta_pago),
  venta_idem = (SELECT COUNT_BIG(1) FROM dbo.venta_idempotencia),
  venta_audit = (SELECT COUNT_BIG(1) FROM dbo.venta_auditoria),
  inventario_stock = (SELECT CONVERT(decimal(18,2), COALESCE(SUM(CONVERT(decimal(18,2), stock)),0)) FROM dbo.inventario),
  cliente_saldo = (SELECT CONVERT(decimal(18,2), COALESCE(SUM(CONVERT(decimal(18,2), saldo)),0)) FROM dbo.cliente),
  movimientos = (SELECT COUNT_BIG(1) FROM dbo.movimiento_caja),
  caja_turnos = (SELECT COUNT_BIG(1) FROM dbo.caja_turno),
  caja_idem = (SELECT COUNT_BIG(1) FROM dbo.caja_idempotencia),
  ingresos_hist = (SELECT COUNT_BIG(1) FROM dbo.ingreso_caja),
  retiros_hist = (SELECT COUNT_BIG(1) FROM dbo.retiro_caja),
  cierres_hist = (SELECT COUNT_BIG(1) FROM dbo.cierre_caja),
  test_product_ok = (SELECT COUNT_BIG(1) FROM dbo.inventario WHERE idProducto=N'API_TEST_PROD_STOCK_ALTO' AND stock >= 1 AND precio > 0),
  test_client_ok = (SELECT COUNT_BIG(1) FROM dbo.cliente WHERE nombre=N'API_TEST_CLIENTE_EFECTIVO');";
await using var reader = await cmd.ExecuteReaderAsync();
var table = new DataTable();
table.Load(reader);
var row = table.Rows[0];
var result = table.Columns.Cast<DataColumn>().ToDictionary(c => c.ColumnName, c => row[c]);
Console.WriteLine(JsonSerializer.Serialize(result));
