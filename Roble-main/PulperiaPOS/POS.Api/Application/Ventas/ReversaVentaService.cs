using Microsoft.Extensions.Options;
using POS.Api.Configuration;
using POS.Api.Contracts.Ventas;
using System.Security.Cryptography;
using System.Text.Json;

namespace POS.Api.Application.Ventas;

public sealed class ReversaVentaService : IReversaVentaService
{
    private const int MaxMotivoLength = 250;
    private static readonly JsonSerializerOptions HashJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly FeatureFlagsOptions featureFlags;
    private readonly IDatabaseEnvironmentSafetyService environmentSafetyService;
    private readonly IReversaVentaRepository reversaVentaRepository;

    public ReversaVentaService(
        IOptions<FeatureFlagsOptions> featureFlags,
        IDatabaseEnvironmentSafetyService environmentSafetyService,
        IReversaVentaRepository reversaVentaRepository)
    {
        this.featureFlags = featureFlags.Value;
        this.environmentSafetyService = environmentSafetyService;
        this.reversaVentaRepository = reversaVentaRepository;
    }

    public async Task<ReversaVentaServiceResult> ReversarVentaEfectivoAsync(
        int factura,
        ReversarVentaRequest? request,
        int usuarioId,
        string traceId,
        CancellationToken cancellationToken)
    {
        if (!featureFlags.EnableVentasApiWrite ||
            !featureFlags.EnableCajaApiWrite ||
            !featureFlags.EnableVentasApiReversaCajaWrite)
        {
            return ReversaVentaServiceResult.Disabled();
        }

        if (!await environmentSafetyService.CanWriteVentasAsync(cancellationToken))
        {
            return ReversaVentaServiceResult.Disabled();
        }

        var errors = ValidateRequestShape(factura, request);
        if (errors.Count > 0)
        {
            return ReversaVentaServiceResult.Invalid(errors);
        }

        var preparedCommand = new ReversarVentaPreparedCommand(
            factura,
            request!,
            usuarioId,
            ComputeRequestHash(factura, request!),
            traceId);

        return await reversaVentaRepository.ReverseVentaEfectivoTransactionalAsync(preparedCommand, cancellationToken);
    }

    private static IReadOnlyCollection<string> ValidateRequestShape(int factura, ReversarVentaRequest? request)
    {
        var errors = new List<string>();

        if (factura <= 0)
        {
            errors.Add("Venta invalida.");
        }

        if (request is null)
        {
            errors.Add("Solicitud de reversa requerida.");
            return errors;
        }

        if (request.IdempotencyKey is null || request.IdempotencyKey == Guid.Empty)
        {
            errors.Add("Idempotency key requerida.");
        }

        if (string.IsNullOrWhiteSpace(request.Motivo))
        {
            errors.Add("Motivo requerido.");
        }
        else if (request.Motivo.Length > MaxMotivoLength)
        {
            errors.Add("Motivo excede la longitud permitida.");
        }

        return errors;
    }

    private static byte[] ComputeRequestHash(int factura, ReversarVentaRequest request)
    {
        var canonical = new
        {
            factura,
            idempotencyKey = request.IdempotencyKey,
            motivo = request.Motivo?.Trim()
        };

        var bytes = JsonSerializer.SerializeToUtf8Bytes(canonical, HashJsonOptions);
        return SHA256.HashData(bytes);
    }
}
