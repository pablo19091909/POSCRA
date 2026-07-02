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
if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("Conexion no configurada.");
await using var conn = new SqlConnection(connectionString);
await conn.OpenAsync();
await using var cmd = conn.CreateCommand();
cmd.CommandText = @"
SET NOCOUNT ON;
DECLARE @idTurno bigint = (SELECT TOP (1) idTurno FROM dbo.caja_turno WHERE caja_codigo=N'CAJA_PRINCIPAL_TEST' ORDER BY idTurno DESC);
SELECT
  environment_ok = CONVERT(int, CASE WHEN EXISTS (SELECT 1 FROM dbo.app_environment WHERE id=1 AND environment_name=N'Test' AND writes_allowed_for_testing=1) THEN 1 ELSE 0 END),
  open_turns = (SELECT COUNT_BIG(1) FROM dbo.caja_turno WHERE caja_codigo=N'CAJA_PRINCIPAL_TEST' AND estado=N'Abierto'),
  closing_turns = (SELECT COUNT_BIG(1) FROM dbo.caja_turno WHERE caja_codigo=N'CAJA_PRINCIPAL_TEST' AND estado=N'EnCierre'),
  last_turn_closed = (SELECT CONVERT(int, CASE WHEN estado=N'Cerrado' THEN 1 ELSE 0 END) FROM dbo.caja_turno WHERE idTurno=@idTurno),
  efectivo_esperado = (SELECT CONVERT(decimal(18,2), efectivo_esperado) FROM dbo.caja_turno WHERE idTurno=@idTurno),
  efectivo_contado = (SELECT CONVERT(decimal(18,2), efectivo_contado) FROM dbo.caja_turno WHERE idTurno=@idTurno),
  diferencia = (SELECT CONVERT(decimal(18,2), diferencia) FROM dbo.caja_turno WHERE idTurno=@idTurno),
  ventas = (SELECT COUNT_BIG(1) FROM dbo.ventas),
  detalle = (SELECT COUNT_BIG(1) FROM dbo.DetalleVenta),
  pagos = (SELECT COUNT_BIG(1) FROM dbo.venta_pago),
  venta_idem = (SELECT COUNT_BIG(1) FROM dbo.venta_idempotencia),
  venta_audit = (SELECT COUNT_BIG(1) FROM dbo.venta_auditoria),
  movimientos = (SELECT COUNT_BIG(1) FROM dbo.movimiento_caja),
  caja_turnos = (SELECT COUNT_BIG(1) FROM dbo.caja_turno),
  caja_idem = (SELECT COUNT_BIG(1) FROM dbo.caja_idempotencia),
  ingresos_hist = (SELECT COUNT_BIG(1) FROM dbo.ingreso_caja),
  retiros_hist = (SELECT COUNT_BIG(1) FROM dbo.retiro_caja),
  cierres_hist = (SELECT COUNT_BIG(1) FROM dbo.cierre_caja),
  fondo = (SELECT COUNT_BIG(1) FROM dbo.movimiento_caja WHERE idTurno=@idTurno AND tipo_movimiento=N'FondoInicial'),
  venta_efectivo = (SELECT COUNT_BIG(1) FROM dbo.movimiento_caja WHERE idTurno=@idTurno AND tipo_movimiento=N'VentaEfectivo'),
  venta_efectivo_total = (SELECT CONVERT(decimal(18,2), COALESCE(SUM(monto),0)) FROM dbo.movimiento_caja WHERE idTurno=@idTurno AND tipo_movimiento=N'VentaEfectivo'),
  ingresos = (SELECT COUNT_BIG(1) FROM dbo.movimiento_caja WHERE idTurno=@idTurno AND tipo_movimiento=N'IngresoCaja'),
  retiros = (SELECT COUNT_BIG(1) FROM dbo.movimiento_caja WHERE idTurno=@idTurno AND tipo_movimiento=N'RetiroCaja'),
  ajustes = (SELECT COUNT_BIG(1) FROM dbo.movimiento_caja WHERE idTurno=@idTurno AND tipo_movimiento LIKE N'Ajuste%'),
  reversas = (SELECT COUNT_BIG(1) FROM dbo.movimiento_caja WHERE idTurno=@idTurno AND tipo_movimiento LIKE N'Reversa%'),
  cierre_diff = (SELECT COUNT_BIG(1) FROM dbo.movimiento_caja WHERE idTurno=@idTurno AND tipo_movimiento=N'CierreDiferencia'),
  cerrar_turno_completada = (SELECT COUNT_BIG(1) FROM dbo.caja_idempotencia WHERE idTurno=@idTurno AND operacion=N'CerrarTurno' AND estado=N'Completada'),
  caja_idem_proceso = (SELECT COUNT_BIG(1) FROM dbo.caja_idempotencia WHERE estado=N'EnProceso'),
  caja_idem_fallida = (SELECT COUNT_BIG(1) FROM dbo.caja_idempotencia WHERE estado=N'Fallida'),
  venta_idem_proceso = (SELECT COUNT_BIG(1) FROM dbo.venta_idempotencia WHERE estado=N'EnProceso'),
  venta_idem_fallida = (SELECT COUNT_BIG(1) FROM dbo.venta_idempotencia WHERE estado=N'Fallida'),
  cliente_saldo = (SELECT CONVERT(decimal(18,2), COALESCE(SUM(CONVERT(decimal(18,2), saldo)),0)) FROM dbo.cliente);
";
await using var reader = await cmd.ExecuteReaderAsync();
var table = new DataTable();
table.Load(reader);
var row = table.Rows[0];
var result = table.Columns.Cast<DataColumn>().ToDictionary(c => c.ColumnName, c => row[c] is DBNull ? null : row[c]);
Console.WriteLine(JsonSerializer.Serialize(result));
