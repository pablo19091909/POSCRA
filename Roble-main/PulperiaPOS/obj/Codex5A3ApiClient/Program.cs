using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;

var mode = args.Length > 0 ? args[0] : "pre";
var baseUrl = "https://127.0.0.1:7046";
var http = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator }) { BaseAddress = new Uri(baseUrl) };

string? JsonValue(string path, params string[] keys)
{
    if (!File.Exists(path)) return null;
    using var doc = JsonDocument.Parse(File.ReadAllText(path));
    var e = doc.RootElement;
    foreach (var key in keys)
    {
        if (!e.TryGetProperty(key, out e)) return null;
    }
    return e.ValueKind == JsonValueKind.String ? e.GetString() : e.ToString();
}

string? ConfigValue(params string[] keys)
{
    var dev = JsonValue(@"POS.Api\appsettings.Development.json", keys);
    if (!string.IsNullOrWhiteSpace(dev)) return dev;
    return JsonValue(@"POS.Api\appsettings.json", keys);
}

var cs = Environment.GetEnvironmentVariable("POS_API_DATABASE_CONNECTION_STRING") ?? ConfigValue("ConnectionStrings", "PosDatabase");
if (string.IsNullOrWhiteSpace(cs)) throw new InvalidOperationException("Conexion no configurada.");
var signingKey = Environment.GetEnvironmentVariable("POS_API_JWT_SIGNING_KEY") ?? ConfigValue("Jwt", "SigningKey");
var issuer = ConfigValue("Jwt", "Issuer") ?? "POS.Api";
var audience = ConfigValue("Jwt", "Audience") ?? "PulperiaPOS.WPF";
if (string.IsNullOrWhiteSpace(signingKey) || signingKey.Length < 32) throw new InvalidOperationException("JWT no configurado.");

async Task<Dictionary<string, object?>> QueryAsync(string sql, Action<SqlParameterCollection>? bind = null)
{
    await using var conn = new SqlConnection(cs);
    await conn.OpenAsync();
    await using var cmd = new SqlCommand(sql, conn);
    bind?.Invoke(cmd.Parameters);
    await using var reader = await cmd.ExecuteReaderAsync();
    var table = new DataTable();
    table.Load(reader);
    var row = table.Rows[0];
    return table.Columns.Cast<DataColumn>().ToDictionary(c => c.ColumnName, c => row[c] is DBNull ? null : row[c]);
}

async Task<(int AdminId, string AdminName, int NoPermId, string ProductId, int ClientId, decimal Price, decimal Stock)> LoadTestDataAsync()
{
    await using var conn = new SqlConnection(cs);
    await conn.OpenAsync();
    var sql = @"
SELECT TOP (1) idUsuario, nombre FROM dbo.usuario WHERE activo=1 AND rol=N'Administrador' ORDER BY idUsuario;
SELECT TOP (1) idUsuario FROM dbo.usuario WHERE activo=1 AND rol NOT IN (N'Administrador', N'Anfitrion') ORDER BY idUsuario;
SELECT TOP (1) idProducto, precio, stock FROM dbo.inventario WHERE idProducto=N'API_TEST_PROD_STOCK_ALTO' AND stock >= 1 AND precio > 0 ORDER BY idProducto;
SELECT TOP (1) idCliente FROM dbo.cliente WHERE nombre=N'API_TEST_CLIENTE_EFECTIVO' ORDER BY idCliente;";
    await using var cmd = new SqlCommand(sql, conn);
    await using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync()) throw new InvalidOperationException("Usuario autorizado no disponible.");
    var adminId = reader.GetInt32(0);
    var adminName = reader.GetString(1);
    await reader.NextResultAsync();
    int noPermId = 0;
    if (await reader.ReadAsync()) noPermId = reader.GetInt32(0);
    await reader.NextResultAsync();
    if (!await reader.ReadAsync()) throw new InvalidOperationException("Producto sintetico no disponible.");
    var productId = reader.GetString(0);
    var price = reader.GetDecimal(1);
    var stock = Convert.ToDecimal(reader.GetValue(2));
    await reader.NextResultAsync();
    if (!await reader.ReadAsync()) throw new InvalidOperationException("Cliente sintetico no disponible.");
    var clientId = reader.GetInt32(0);
    return (adminId, adminName, noPermId, productId, clientId, price, stock);
}

string Token(int userId, string username, string role, IEnumerable<string> permissions)
{
    var claims = new List<Claim>
    {
        new("userId", userId.ToString()),
        new("username", username),
        new("role", role),
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
    };
    claims.AddRange(permissions.Select(p => new Claim("permission", p)));
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
    var jwt = new JwtSecurityToken(issuer, audience, claims, expires: DateTime.UtcNow.AddMinutes(30), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
    return new JwtSecurityTokenHandler().WriteToken(jwt);
}

StringContent JsonBody(object body) => new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
async Task<int> StatusAsync(HttpRequestMessage request)
{
    using var resp = await http.SendAsync(request);
    return (int)resp.StatusCode;
}
async Task<(int Status, JsonDocument? Body)> SendJsonAsync(HttpRequestMessage request)
{
    using var resp = await http.SendAsync(request);
    var text = await resp.Content.ReadAsStringAsync();
    return ((int)resp.StatusCode, string.IsNullOrWhiteSpace(text) ? null : JsonDocument.Parse(text));
}

var data = await LoadTestDataAsync();
var adminToken = Token(data.AdminId, data.AdminName, "Administrador", new[] { "Ventas.Crear", "Caja.Abrir", "Caja.Ver" });
var noPermToken = data.NoPermId > 0 ? Token(data.NoPermId, "no-perm", "SinPermiso", Array.Empty<string>()) : Token(data.AdminId, data.AdminName, "Administrador", Array.Empty<string>());
object SaleBody(Guid key, int quantity = 1, string? productOverride = null) => new
{
    clienteId = data.ClientId,
    items = new[] { new { productoId = productOverride ?? data.ProductId, cantidad = quantity } },
    pago = new { metodoPago = "Efectivo", montoRecibido = data.Price * quantity, moneda = "CRC" },
    idempotencyKey = key,
    observaciones = "Fase 5A.3 venta efectivo API Test"
};

if (mode == "pre")
{
    var health = await StatusAsync(new HttpRequestMessage(HttpMethod.Get, "/health"));
    var db = await StatusAsync(new HttpRequestMessage(HttpMethod.Get, "/health/database"));
    var version = await StatusAsync(new HttpRequestMessage(HttpMethod.Get, "/api/system/version"));
    var unauth = await StatusAsync(new HttpRequestMessage(HttpMethod.Post, "/api/ventas") { Content = JsonBody(SaleBody(Guid.NewGuid())) });
    var noPermReq = new HttpRequestMessage(HttpMethod.Post, "/api/ventas") { Content = JsonBody(SaleBody(Guid.NewGuid())) };
    noPermReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", noPermToken);
    var noPerm = await StatusAsync(noPermReq);
    var blockedReq = new HttpRequestMessage(HttpMethod.Post, "/api/ventas") { Content = JsonBody(SaleBody(Guid.NewGuid())) };
    blockedReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
    var blocked = await StatusAsync(blockedReq);
    Console.WriteLine(JsonSerializer.Serialize(new { health, database = db, version, unauth, noPerm, blockedWithFlagsOff = blocked, productReady = data.Stock >= 1, saleTotal = data.Price }));
    return;
}

if (mode == "execute")
{
    var before = await QueryAsync(@"
SELECT ventas=(SELECT COUNT_BIG(1) FROM dbo.ventas), detalle=(SELECT COUNT_BIG(1) FROM dbo.DetalleVenta), pagos=(SELECT COUNT_BIG(1) FROM dbo.venta_pago), venta_idem=(SELECT COUNT_BIG(1) FROM dbo.venta_idempotencia), venta_audit=(SELECT COUNT_BIG(1) FROM dbo.venta_auditoria), movimientos=(SELECT COUNT_BIG(1) FROM dbo.movimiento_caja), caja_turnos=(SELECT COUNT_BIG(1) FROM dbo.caja_turno), caja_idem=(SELECT COUNT_BIG(1) FROM dbo.caja_idempotencia), stock=(SELECT CONVERT(decimal(18,2), stock) FROM dbo.inventario WHERE idProducto=N'API_TEST_PROD_STOCK_ALTO'), ingresos_hist=(SELECT COUNT_BIG(1) FROM dbo.ingreso_caja), retiros_hist=(SELECT COUNT_BIG(1) FROM dbo.retiro_caja), cierres_hist=(SELECT COUNT_BIG(1) FROM dbo.cierre_caja);");
    var openKey = Guid.NewGuid().ToString();
    var openReq = new HttpRequestMessage(HttpMethod.Post, "/api/caja/turnos/abrir") { Content = JsonBody(new { cajaCodigo = "CAJA_PRINCIPAL_TEST", fondoInicial = 1000.00m, observacion = "Turno sintetico para prueba API VentaEfectivo Fase 5A.3" }) };
    openReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
    openReq.Headers.TryAddWithoutValidation("Idempotency-Key", openKey);
    var open = await SendJsonAsync(openReq);
    long idTurno = open.Body?.RootElement.GetProperty("idTurno").GetInt64() ?? 0;
    var saleKey = Guid.NewGuid();
    var saleReq = new HttpRequestMessage(HttpMethod.Post, "/api/ventas") { Content = JsonBody(SaleBody(saleKey)) };
    saleReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
    var sale = await SendJsonAsync(saleReq);
    decimal saleTotal = sale.Body?.RootElement.GetProperty("total").GetDecimal() ?? 0m;
    string saleResult = sale.Body?.RootElement.GetProperty("resultadoIdempotencia").GetString() ?? "";
    var retryReq = new HttpRequestMessage(HttpMethod.Post, "/api/ventas") { Content = JsonBody(SaleBody(saleKey)) };
    retryReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
    var retry = await SendJsonAsync(retryReq);
    string retryResult = retry.Body?.RootElement.GetProperty("resultadoIdempotencia").GetString() ?? "";
    var conflictReq = new HttpRequestMessage(HttpMethod.Post, "/api/ventas") { Content = JsonBody(SaleBody(saleKey, 2)) };
    conflictReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
    var conflict = await StatusAsync(conflictReq);
    var rollbackReq = new HttpRequestMessage(HttpMethod.Post, "/api/ventas") { Content = JsonBody(SaleBody(Guid.NewGuid(), 1, "API_TEST_PROD_NO_EXISTE")) };
    rollbackReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
    var rollback = await StatusAsync(rollbackReq);
    var after = await QueryAsync(@"
DECLARE @idTurno bigint = (SELECT TOP (1) idTurno FROM dbo.caja_turno WHERE caja_codigo=N'CAJA_PRINCIPAL_TEST' AND estado=N'Abierto' ORDER BY idTurno DESC);
SELECT ventas=(SELECT COUNT_BIG(1) FROM dbo.ventas), detalle=(SELECT COUNT_BIG(1) FROM dbo.DetalleVenta), pagos=(SELECT COUNT_BIG(1) FROM dbo.venta_pago), venta_idem=(SELECT COUNT_BIG(1) FROM dbo.venta_idempotencia), venta_audit=(SELECT COUNT_BIG(1) FROM dbo.venta_auditoria), movimientos=(SELECT COUNT_BIG(1) FROM dbo.movimiento_caja), caja_turnos=(SELECT COUNT_BIG(1) FROM dbo.caja_turno), caja_idem=(SELECT COUNT_BIG(1) FROM dbo.caja_idempotencia), stock=(SELECT CONVERT(decimal(18,2), stock) FROM dbo.inventario WHERE idProducto=N'API_TEST_PROD_STOCK_ALTO'), ingresos_hist=(SELECT COUNT_BIG(1) FROM dbo.ingreso_caja), retiros_hist=(SELECT COUNT_BIG(1) FROM dbo.retiro_caja), cierres_hist=(SELECT COUNT_BIG(1) FROM dbo.cierre_caja), open_turns=(SELECT COUNT_BIG(1) FROM dbo.caja_turno WHERE caja_codigo=N'CAJA_PRINCIPAL_TEST' AND estado=N'Abierto'), closing_turns=(SELECT COUNT_BIG(1) FROM dbo.caja_turno WHERE caja_codigo=N'CAJA_PRINCIPAL_TEST' AND estado=N'EnCierre'), fondo=(SELECT COUNT_BIG(1) FROM dbo.movimiento_caja WHERE idTurno=@idTurno AND tipo_movimiento=N'FondoInicial'), venta_efectivo=(SELECT COUNT_BIG(1) FROM dbo.movimiento_caja WHERE idTurno=@idTurno AND tipo_movimiento=N'VentaEfectivo'), ingresos=(SELECT COUNT_BIG(1) FROM dbo.movimiento_caja WHERE idTurno=@idTurno AND tipo_movimiento=N'IngresoCaja'), retiros=(SELECT COUNT_BIG(1) FROM dbo.movimiento_caja WHERE idTurno=@idTurno AND tipo_movimiento=N'RetiroCaja'), ajustes=(SELECT COUNT_BIG(1) FROM dbo.movimiento_caja WHERE idTurno=@idTurno AND tipo_movimiento LIKE N'Ajuste%'), reversas=(SELECT COUNT_BIG(1) FROM dbo.movimiento_caja WHERE idTurno=@idTurno AND tipo_movimiento LIKE N'Reversa%'), cierre_diff=(SELECT COUNT_BIG(1) FROM dbo.movimiento_caja WHERE idTurno=@idTurno AND tipo_movimiento=N'CierreDiferencia'), efectivo_esperado=(SELECT CONVERT(decimal(18,2), efectivo_esperado) FROM dbo.caja_turno WHERE idTurno=@idTurno), caja_idem_proceso=(SELECT COUNT_BIG(1) FROM dbo.caja_idempotencia WHERE estado=N'EnProceso'), caja_idem_fallida=(SELECT COUNT_BIG(1) FROM dbo.caja_idempotencia WHERE estado=N'Fallida'), venta_idem_proceso=(SELECT COUNT_BIG(1) FROM dbo.venta_idempotencia WHERE estado=N'EnProceso'), venta_idem_fallida=(SELECT COUNT_BIG(1) FROM dbo.venta_idempotencia WHERE estado=N'Fallida');");
    long D(string k) => Convert.ToInt64(after[k]!) - Convert.ToInt64(before[k]!);
    decimal stockDelta = Convert.ToDecimal(after["stock"]!) - Convert.ToDecimal(before["stock"]!);
    Console.WriteLine(JsonSerializer.Serialize(new { openStatus = open.Status, saleStatus = sale.Status, saleResult, retryStatus = retry.Status, retryResult, conflictStatus = conflict, rollbackStatus = rollback, saleTotal, deltas = new { ventas = D("ventas"), detalle = D("detalle"), pagos = D("pagos"), ventaIdem = D("venta_idem"), ventaAudit = D("venta_audit"), movimientos = D("movimientos"), cajaTurnos = D("caja_turnos"), cajaIdem = D("caja_idem"), ingresosHist = D("ingresos_hist"), retirosHist = D("retiros_hist"), cierresHist = D("cierres_hist"), stock = stockDelta }, final = after }));
    return;
}

if (mode == "post")
{
    var health = await StatusAsync(new HttpRequestMessage(HttpMethod.Get, "/health"));
    var db = await StatusAsync(new HttpRequestMessage(HttpMethod.Get, "/health/database"));
    var version = await StatusAsync(new HttpRequestMessage(HttpMethod.Get, "/api/system/version"));
    var turnoReq = new HttpRequestMessage(HttpMethod.Get, "/api/caja/turnos/abierto?cajaCodigo=CAJA_PRINCIPAL_TEST");
    turnoReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
    var turno = await SendJsonAsync(turnoReq);
    long turnoId = turno.Body?.RootElement.GetProperty("idTurno").GetInt64() ?? 0;
    var preReq = new HttpRequestMessage(HttpMethod.Get, $"/api/caja/turnos/{turnoId}/pre-cierre");
    preReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
    var pre = await SendJsonAsync(preReq);
    decimal esperado = pre.Body?.RootElement.GetProperty("efectivoEsperado").GetDecimal() ?? -1m;
    var resumen = pre.Body?.RootElement.GetProperty("resumen").EnumerateArray()
        .Select(x => new { tipo = x.GetProperty("tipoMovimiento").GetString(), cantidad = x.GetProperty("cantidad").GetInt64(), total = x.GetProperty("total").GetDecimal() })
        .ToArray() ?? [];
    var final = await QueryAsync(@"
DECLARE @idTurno bigint = (SELECT TOP (1) idTurno FROM dbo.caja_turno WHERE caja_codigo=N'CAJA_PRINCIPAL_TEST' AND estado=N'Abierto' ORDER BY idTurno DESC);
SELECT open_turns=(SELECT COUNT_BIG(1) FROM dbo.caja_turno WHERE caja_codigo=N'CAJA_PRINCIPAL_TEST' AND estado=N'Abierto'), closing_turns=(SELECT COUNT_BIG(1) FROM dbo.caja_turno WHERE caja_codigo=N'CAJA_PRINCIPAL_TEST' AND estado=N'EnCierre'), fondo=(SELECT COUNT_BIG(1) FROM dbo.movimiento_caja WHERE idTurno=@idTurno AND tipo_movimiento=N'FondoInicial'), venta_efectivo=(SELECT COUNT_BIG(1) FROM dbo.movimiento_caja WHERE idTurno=@idTurno AND tipo_movimiento=N'VentaEfectivo'), ingresos=(SELECT COUNT_BIG(1) FROM dbo.movimiento_caja WHERE idTurno=@idTurno AND tipo_movimiento=N'IngresoCaja'), retiros=(SELECT COUNT_BIG(1) FROM dbo.movimiento_caja WHERE idTurno=@idTurno AND tipo_movimiento=N'RetiroCaja'), ajustes=(SELECT COUNT_BIG(1) FROM dbo.movimiento_caja WHERE idTurno=@idTurno AND tipo_movimiento LIKE N'Ajuste%'), reversas=(SELECT COUNT_BIG(1) FROM dbo.movimiento_caja WHERE idTurno=@idTurno AND tipo_movimiento LIKE N'Reversa%'), cierre_diff=(SELECT COUNT_BIG(1) FROM dbo.movimiento_caja WHERE idTurno=@idTurno AND tipo_movimiento=N'CierreDiferencia'), caja_idem_proceso=(SELECT COUNT_BIG(1) FROM dbo.caja_idempotencia WHERE estado=N'EnProceso'), caja_idem_fallida=(SELECT COUNT_BIG(1) FROM dbo.caja_idempotencia WHERE estado=N'Fallida'), venta_idem_proceso=(SELECT COUNT_BIG(1) FROM dbo.venta_idempotencia WHERE estado=N'EnProceso'), venta_idem_fallida=(SELECT COUNT_BIG(1) FROM dbo.venta_idempotencia WHERE estado=N'Fallida');");
    Console.WriteLine(JsonSerializer.Serialize(new { health, database = db, version, turnoStatus = turno.Status, preCierreStatus = pre.Status, efectivoEsperado = esperado, resumen, final }));
    return;
}
throw new InvalidOperationException("Modo invalido.");

