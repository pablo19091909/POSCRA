using Microsoft.Extensions.Options;
using POS.Api.Configuration;
using POS.Api.Contracts.Ventas;

namespace POS.Api.Application.Ventas;

public sealed class VentaService : IVentaService
{
    private const int MaxItems = 100;
    private const int MaxCantidad = 10000;
    private const int MaxObservacionesLength = 250;
    private const int MaxReferenciaLength = 100;

    private static readonly HashSet<string> SupportedPaymentMethods = new(StringComparer.Ordinal)
    {
        "Efectivo",
        "Tarjeta",
        "Sinpe",
        "Dolares",
        "SaldoCliente"
    };

    private readonly FeatureFlagsOptions featureFlags;
    private readonly IIdempotenciaVentaService idempotenciaVentaService;
    private readonly IVentaRepository ventaRepository;
    private readonly IDatabaseEnvironmentSafetyService environmentSafetyService;

    public VentaService(
        IOptions<FeatureFlagsOptions> featureFlags,
        IIdempotenciaVentaService idempotenciaVentaService,
        IVentaRepository ventaRepository,
        IDatabaseEnvironmentSafetyService environmentSafetyService)
    {
        this.featureFlags = featureFlags.Value;
        this.idempotenciaVentaService = idempotenciaVentaService;
        this.ventaRepository = ventaRepository;
        this.environmentSafetyService = environmentSafetyService;
    }

    public async Task<VentaServiceResult> CrearVentaAsync(
        CrearVentaRequest? request,
        int usuarioId,
        string traceId,
        CancellationToken cancellationToken)
    {
        if (!featureFlags.EnableVentasApiWrite)
        {
            return VentaServiceResult.Disabled();
        }

        if (IsEfectivoRequest(request) &&
            (!featureFlags.EnableCajaApiWrite || !featureFlags.EnableVentasApiEfectivoCajaWrite))
        {
            return VentaServiceResult.Disabled();
        }

        if (!await environmentSafetyService.CanWriteVentasAsync(cancellationToken))
        {
            return VentaServiceResult.Disabled();
        }

        var errors = ValidateRequestShape(request);
        if (errors.Count > 0)
        {
            return VentaServiceResult.Invalid(errors);
        }

        var requestHash = idempotenciaVentaService.ComputeRequestHash(request!);
        var command = new CrearVentaPreparedCommand(
            request!,
            usuarioId,
            requestHash,
            traceId,
            IntegrarCajaEfectivo: IsEfectivoRequest(request));

        return await ventaRepository.CreateVentaTransactionalAsync(command, cancellationToken);
    }

    private static bool IsEfectivoRequest(CrearVentaRequest? request)
    {
        return string.Equals(
            request?.Pago?.MetodoPago?.Trim(),
            "Efectivo",
            StringComparison.Ordinal);
    }

    private static IReadOnlyCollection<string> ValidateRequestShape(CrearVentaRequest? request)
    {
        var errors = new List<string>();
        if (request is null)
        {
            errors.Add("Solicitud de venta requerida.");
            return errors;
        }

        if (request.ClienteId < 0)
        {
            errors.Add("Cliente invalido.");
        }

        if (request.IdempotencyKey is null || request.IdempotencyKey == Guid.Empty)
        {
            errors.Add("Idempotency key requerida.");
        }

        if (request.Observaciones?.Length > MaxObservacionesLength)
        {
            errors.Add("Observaciones exceden la longitud permitida.");
        }

        ValidateText(request.ReferenciaPago, "Referencia de pago invalida.", errors);
        ValidateText(request.Voucher, "Voucher invalido.", errors);
        ValidateItems(request.Items, errors);
        ValidatePago(request.Pago, request.TipoCambioObservado, errors);

        return errors;
    }

    private static void ValidateItems(IReadOnlyCollection<VentaItemRequest>? items, List<string> errors)
    {
        if (items is null || items.Count == 0)
        {
            errors.Add("La venta requiere al menos un item.");
            return;
        }

        if (items.Count > MaxItems)
        {
            errors.Add("Cantidad de items excede el limite permitido.");
        }

        var productIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.ProductoId) || item.ProductoId.Length > 100)
            {
                errors.Add("Producto invalido.");
                continue;
            }

            if (!productIds.Add(item.ProductoId.Trim()))
            {
                errors.Add("No se permiten productos duplicados en esta version.");
            }

            if (item.Cantidad <= 0 || item.Cantidad > MaxCantidad)
            {
                errors.Add("Cantidad de producto invalida.");
            }
        }
    }

    private static void ValidatePago(PagoVentaRequest? pago, decimal? tipoCambioObservado, List<string> errors)
    {
        if (pago is null)
        {
            errors.Add("Pago requerido.");
            return;
        }

        if (string.IsNullOrWhiteSpace(pago.MetodoPago))
        {
            errors.Add("Metodo de pago requerido.");
            return;
        }

        var metodo = pago.MetodoPago.Trim();
        if (string.Equals(metodo, "Donación", StringComparison.Ordinal) ||
            string.Equals(metodo, "Donacion", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Donacion no esta soportada por la venta API inicial.");
            return;
        }

        if (!SupportedPaymentMethods.Contains(metodo))
        {
            errors.Add("Metodo de pago no soportado.");
        }

        if (pago.MontoRecibido is not null && pago.MontoRecibido <= 0)
        {
            errors.Add("Monto recibido invalido.");
        }

        ValidateText(pago.Referencia, "Referencia de pago invalida.", errors);
        ValidateText(pago.Voucher, "Voucher invalido.", errors);

        if (!string.IsNullOrWhiteSpace(pago.Moneda) &&
            !string.Equals(pago.Moneda, "CRC", StringComparison.Ordinal) &&
            !string.Equals(pago.Moneda, "USD", StringComparison.Ordinal))
        {
            errors.Add("Moneda no soportada.");
        }

        if (string.Equals(metodo, "Dolares", StringComparison.Ordinal) &&
            (pago.TipoCambioObservado is null || pago.TipoCambioObservado <= 0) &&
            (tipoCambioObservado is null || tipoCambioObservado <= 0))
        {
            errors.Add("Tipo de cambio requerido para pago en dolares.");
        }
    }

    private static void ValidateText(string? value, string error, List<string> errors)
    {
        if (value?.Length > MaxReferenciaLength)
        {
            errors.Add(error);
        }
    }
}
