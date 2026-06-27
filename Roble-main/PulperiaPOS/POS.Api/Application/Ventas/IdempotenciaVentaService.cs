using System.Security.Cryptography;
using System.Text.Json;
using POS.Api.Contracts.Ventas;

namespace POS.Api.Application.Ventas;

public sealed class IdempotenciaVentaService : IIdempotenciaVentaService
{
    private static readonly JsonSerializerOptions HashJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly IVentaRepository ventaRepository;

    public IdempotenciaVentaService(IVentaRepository ventaRepository)
    {
        this.ventaRepository = ventaRepository;
    }

    public byte[] ComputeRequestHash(CrearVentaRequest request)
    {
        var canonical = new
        {
            clienteId = request.ClienteId,
            items = (request.Items ?? [])
                .Select(item => new
                {
                    productoId = item.ProductoId?.Trim(),
                    cantidad = item.Cantidad
                })
                .OrderBy(item => item.productoId, StringComparer.Ordinal)
                .ToArray(),
            pago = request.Pago is null
                ? null
                : new
                {
                    metodoPago = request.Pago.MetodoPago?.Trim(),
                    montoRecibido = request.Pago.MontoRecibido,
                    referencia = request.Pago.Referencia?.Trim(),
                    voucher = request.Pago.Voucher?.Trim(),
                    moneda = request.Pago.Moneda?.Trim(),
                    tipoCambioObservado = request.Pago.TipoCambioObservado
                },
            observaciones = request.Observaciones?.Trim(),
            tipoCambioObservado = request.TipoCambioObservado,
            referenciaPago = request.ReferenciaPago?.Trim(),
            voucher = request.Voucher?.Trim()
        };

        var bytes = JsonSerializer.SerializeToUtf8Bytes(canonical, HashJsonOptions);
        return SHA256.HashData(bytes);
    }

    public Task<VentaIdempotenciaState?> FindAsync(
        int usuarioId,
        Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        return ventaRepository.GetIdempotenciaAsync(usuarioId, idempotencyKey, cancellationToken);
    }
}
