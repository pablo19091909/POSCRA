using System.Security.Cryptography;
using System.Globalization;
using System.Text.Json;
using POS.Api.Contracts.Caja;

namespace POS.Api.Application.Caja;

public sealed class CajaIdempotencyService : ICajaIdempotencyService
{
    private static readonly JsonSerializerOptions HashJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public bool TryParseKey(string? value, out Guid idempotencyKey)
    {
        return Guid.TryParse(value, out idempotencyKey) && idempotencyKey != Guid.Empty;
    }

    public byte[] ComputeAbrirTurnoRequestHash(AbrirCajaTurnoRequest request, int usuarioId)
    {
        var canonical = new
        {
            operacion = CajaIdempotencyOperation.AbrirTurno.ToString(),
            usuarioId,
            cajaCodigo = NormalizeCajaCodigo(request.CajaCodigo),
            fondoInicial = request.FondoInicial.ToString("0.00", CultureInfo.InvariantCulture),
            observacion = NormalizeText(request.Observacion)
        };

        var bytes = JsonSerializer.SerializeToUtf8Bytes(canonical, HashJsonOptions);
        return SHA256.HashData(bytes);
    }

    public byte[] ComputeIngresoRequestHash(RegistrarIngresoCajaRequest request, int usuarioId)
    {
        var canonical = new
        {
            operacion = CajaIdempotencyOperation.IngresoCaja.ToString(),
            usuarioId,
            cajaCodigo = NormalizeCajaCodigo(request.CajaCodigo),
            monto = request.Monto.ToString("0.00", CultureInfo.InvariantCulture),
            motivo = request.Motivo?.Trim(),
            referencia = request.Referencia?.Trim()
        };

        var bytes = JsonSerializer.SerializeToUtf8Bytes(canonical, HashJsonOptions);
        return SHA256.HashData(bytes);
    }

    public byte[] ComputeRetiroRequestHash(RegistrarRetiroCajaRequest request, int usuarioId)
    {
        var canonical = new
        {
            operacion = CajaIdempotencyOperation.RetiroCaja.ToString(),
            usuarioId,
            cajaCodigo = NormalizeCajaCodigo(request.CajaCodigo),
            monto = request.Monto.ToString("0.00", CultureInfo.InvariantCulture),
            motivo = request.Motivo?.Trim(),
            referencia = request.Referencia?.Trim()
        };

        var bytes = JsonSerializer.SerializeToUtf8Bytes(canonical, HashJsonOptions);
        return SHA256.HashData(bytes);
    }

    public byte[] ComputeCerrarTurnoRequestHash(
        long idTurno,
        string cajaCodigo,
        CerrarCajaTurnoRequest request,
        int usuarioId)
    {
        var canonical = new
        {
            operacion = CajaIdempotencyOperation.CerrarTurno.ToString(),
            usuarioId,
            idTurno,
            cajaCodigo = NormalizeCajaCodigo(cajaCodigo),
            efectivoContado = request.EfectivoContado.ToString("0.00", CultureInfo.InvariantCulture),
            observacion = NormalizeText(request.Observacion),
            rowVersion = NormalizeText(request.RowVersion)
        };

        var bytes = JsonSerializer.SerializeToUtf8Bytes(canonical, HashJsonOptions);
        return SHA256.HashData(bytes);
    }

    private static string? NormalizeCajaCodigo(string? cajaCodigo)
    {
        return cajaCodigo?.Trim().ToUpperInvariant();
    }

    private static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
