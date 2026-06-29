using Microsoft.Extensions.Options;
using POS.Api.Application.Ventas;
using POS.Api.Configuration;
using POS.Api.Contracts.Caja;

namespace POS.Api.Application.Caja;

public sealed class CajaService : ICajaService
{
    private const int MaxCajaCodigoLength = 50;
    private const int MaxTextLength = 250;
    private const int MaxMovimientoLimit = 200;
    private const decimal MaxMoneyAmount = 9999999999999999.99m;

    private readonly FeatureFlagsOptions featureFlags;
    private readonly IDatabaseEnvironmentSafetyService environmentSafetyService;
    private readonly ICajaRepository cajaRepository;
    private readonly ICajaIdempotencyService cajaIdempotencyService;

    public CajaService(
        IOptions<FeatureFlagsOptions> featureFlags,
        IDatabaseEnvironmentSafetyService environmentSafetyService,
        ICajaRepository cajaRepository,
        ICajaIdempotencyService cajaIdempotencyService)
    {
        this.featureFlags = featureFlags.Value;
        this.environmentSafetyService = environmentSafetyService;
        this.cajaRepository = cajaRepository;
        this.cajaIdempotencyService = cajaIdempotencyService;
    }

    public async Task<CajaServiceResult<CajaTurnoResponse?>> GetTurnoAbiertoAsync(
        string? cajaCodigo,
        CancellationToken cancellationToken)
    {
        var errors = ValidateCajaCodigo(cajaCodigo);
        if (errors.Count > 0)
        {
            return CajaServiceResult<CajaTurnoResponse?>.Invalid(errors);
        }

        var turno = await cajaRepository.GetTurnoAbiertoAsync(cajaCodigo!.Trim(), cancellationToken);
        return CajaServiceResult<CajaTurnoResponse?>.Success(turno is null ? null : MapTurno(turno));
    }

    public async Task<CajaServiceResult<IReadOnlyCollection<MovimientoCajaResponse>>> GetMovimientosAsync(
        long idTurno,
        CancellationToken cancellationToken)
    {
        if (idTurno <= 0)
        {
            return CajaServiceResult<IReadOnlyCollection<MovimientoCajaResponse>>.Invalid(["Turno invalido."]);
        }

        var turno = await cajaRepository.GetTurnoByIdAsync(idTurno, cancellationToken);
        if (turno is null)
        {
            return CajaServiceResult<IReadOnlyCollection<MovimientoCajaResponse>>.NotFound("Turno no encontrado.");
        }

        var movimientos = await cajaRepository.GetMovimientosAsync(idTurno, MaxMovimientoLimit, cancellationToken);
        return CajaServiceResult<IReadOnlyCollection<MovimientoCajaResponse>>.Success(
            movimientos.Select(MapMovimiento).ToArray());
    }

    public async Task<CajaServiceResult<PreCierreCajaResponse>> GetPreCierreAsync(
        long idTurno,
        CancellationToken cancellationToken)
    {
        if (idTurno <= 0)
        {
            return CajaServiceResult<PreCierreCajaResponse>.Invalid(["Turno invalido."]);
        }

        var turno = await cajaRepository.GetTurnoByIdAsync(idTurno, cancellationToken);
        if (turno is null)
        {
            return CajaServiceResult<PreCierreCajaResponse>.NotFound("Turno no encontrado.");
        }

        if (!string.Equals(turno.Estado, "Abierto", StringComparison.Ordinal))
        {
            return CajaServiceResult<PreCierreCajaResponse>.Conflict("El turno no esta abierto.");
        }

        var efectivoEsperado = await cajaRepository.CalcularEfectivoEsperadoAsync(idTurno, cancellationToken);
        var resumen = await cajaRepository.GetResumenMovimientosAsync(idTurno, cancellationToken);

        var response = new PreCierreCajaResponse(
            turno.IdTurno,
            turno.CajaCodigo,
            turno.Estado,
            ToUtc(turno.AperturaUtc),
            efectivoEsperado,
            resumen.Select(item => new ResumenMovimientoCajaResponse(item.TipoMovimiento, item.Cantidad, item.Total)).ToArray());

        return CajaServiceResult<PreCierreCajaResponse>.Success(response);
    }

    public async Task<CajaServiceResult<CajaTurnoResponse>> AbrirTurnoAsync(
        AbrirCajaTurnoRequest? request,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        if (!await CanWriteCajaAsync(cancellationToken))
        {
            return CajaServiceResult<CajaTurnoResponse>.Disabled();
        }

        var errors = ValidateAbrirTurno(request, usuarioId);
        if (errors.Count > 0)
        {
            return CajaServiceResult<CajaTurnoResponse>.Invalid(errors);
        }

        try
        {
            var turno = await cajaRepository.AbrirTurnoAsync(
                request!.CajaCodigo!.Trim(),
                request.FondoInicial,
                request.Observacion?.Trim(),
                usuarioId,
                cancellationToken);

            return CajaServiceResult<CajaTurnoResponse>.Success(MapTurno(turno));
        }
        catch (CajaBusinessException exception) when (exception.Status == CajaServiceStatus.Conflict)
        {
            return CajaServiceResult<CajaTurnoResponse>.Conflict(exception.SafeMessage);
        }
        catch (CajaBusinessException exception) when (exception.Status == CajaServiceStatus.Invalid)
        {
            return CajaServiceResult<CajaTurnoResponse>.Invalid([exception.SafeMessage]);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return CajaServiceResult<CajaTurnoResponse>.Disabled();
        }
    }

    public async Task<CajaServiceResult<MovimientoCajaResponse>> RegistrarIngresoAsync(
        RegistrarIngresoCajaRequest? request,
        int usuarioId,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!await CanWriteCajaAsync(cancellationToken))
        {
            return CajaServiceResult<MovimientoCajaResponse>.Disabled();
        }

        var errors = ValidateIngreso(request, usuarioId);
        if (errors.Count > 0)
        {
            return CajaServiceResult<MovimientoCajaResponse>.Invalid(errors);
        }

        if (!cajaIdempotencyService.TryParseKey(idempotencyKey, out var parsedIdempotencyKey))
        {
            return CajaServiceResult<MovimientoCajaResponse>.Invalid(["Idempotency-Key invalida."]);
        }

        var requestHash = cajaIdempotencyService.ComputeIngresoRequestHash(request!, usuarioId);

        try
        {
            var movimiento = await cajaRepository.RegistrarIngresoAsync(
                request!.CajaCodigo!.Trim(),
                request.Monto,
                request.Motivo!.Trim(),
                request.Referencia?.Trim(),
                usuarioId,
                parsedIdempotencyKey,
                requestHash,
                cancellationToken);

            return CajaServiceResult<MovimientoCajaResponse>.Success(MapMovimiento(movimiento));
        }
        catch (CajaBusinessException exception) when (exception.Status == CajaServiceStatus.Conflict)
        {
            return CajaServiceResult<MovimientoCajaResponse>.Conflict(exception.SafeMessage);
        }
        catch (CajaBusinessException exception) when (exception.Status == CajaServiceStatus.Invalid)
        {
            return CajaServiceResult<MovimientoCajaResponse>.Invalid([exception.SafeMessage]);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return CajaServiceResult<MovimientoCajaResponse>.Disabled();
        }
    }

    public async Task<CajaServiceResult<MovimientoCajaResponse>> RegistrarRetiroAsync(
        RegistrarRetiroCajaRequest? request,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        if (!await CanWriteCajaAsync(cancellationToken))
        {
            return CajaServiceResult<MovimientoCajaResponse>.Disabled();
        }

        var errors = ValidateMovimientoManual(request?.Monto, request?.Motivo, request?.Referencia, usuarioId);
        return CajaServiceResult<MovimientoCajaResponse>.Invalid(errors);
    }

    public async Task<CajaServiceResult<CierreCajaResponse>> CerrarTurnoAsync(
        long idTurno,
        CerrarCajaTurnoRequest? request,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        if (!await CanWriteCajaAsync(cancellationToken))
        {
            return CajaServiceResult<CierreCajaResponse>.Disabled();
        }

        var errors = ValidateCerrarTurno(idTurno, request, usuarioId);
        return CajaServiceResult<CierreCajaResponse>.Invalid(errors);
    }

    private async Task<bool> CanWriteCajaAsync(CancellationToken cancellationToken)
    {
        if (!featureFlags.EnableCajaApiWrite)
        {
            return false;
        }

        return await environmentSafetyService.CanWriteCajaAsync(cancellationToken);
    }

    private static List<string> ValidateCajaCodigo(string? cajaCodigo)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(cajaCodigo) || cajaCodigo.Trim().Length > MaxCajaCodigoLength)
        {
            errors.Add("Codigo de caja invalido.");
        }

        return errors;
    }

    private static IReadOnlyCollection<string> ValidateAbrirTurno(AbrirCajaTurnoRequest? request, int usuarioId)
    {
        var errors = request is null ? ["Solicitud requerida."] : ValidateCajaCodigo(request.CajaCodigo);
        if (request is null)
        {
            return errors;
        }

        if (usuarioId <= 0)
        {
            errors.Add("Usuario invalido.");
        }

        if (request.FondoInicial <= 0 || request.FondoInicial > MaxMoneyAmount)
        {
            errors.Add("Fondo inicial invalido.");
        }

        ValidateText(request.Observacion, "Observacion invalida.", errors);
        return errors;
    }

    private static IReadOnlyCollection<string> ValidateMovimientoManual(
        decimal? monto,
        string? motivo,
        string? referencia,
        int usuarioId)
    {
        var errors = new List<string>();
        if (usuarioId <= 0)
        {
            errors.Add("Usuario invalido.");
        }

        if (monto is null || monto <= 0)
        {
            errors.Add("Monto invalido.");
        }

        ValidateText(motivo, "Motivo invalido.", errors);
        ValidateText(referencia, "Referencia invalida.", errors);
        return errors;
    }

    private static IReadOnlyCollection<string> ValidateIngreso(RegistrarIngresoCajaRequest? request, int usuarioId)
    {
        var errors = request is null ? ["Solicitud requerida."] : ValidateCajaCodigo(request.CajaCodigo);
        if (request is null)
        {
            return errors;
        }

        if (usuarioId <= 0)
        {
            errors.Add("Usuario invalido.");
        }

        if (request.Monto <= 0 || request.Monto > MaxMoneyAmount)
        {
            errors.Add("Monto invalido.");
        }

        if (string.IsNullOrWhiteSpace(request.Motivo))
        {
            errors.Add("Motivo requerido.");
        }

        ValidateText(request.Motivo, "Motivo invalido.", errors);
        ValidateText(request.Referencia, "Referencia invalida.", errors);
        return errors;
    }

    private static IReadOnlyCollection<string> ValidateCerrarTurno(long idTurno, CerrarCajaTurnoRequest? request, int usuarioId)
    {
        var errors = new List<string>();
        if (idTurno <= 0)
        {
            errors.Add("Turno invalido.");
        }

        if (usuarioId <= 0)
        {
            errors.Add("Usuario invalido.");
        }

        if (request is null)
        {
            errors.Add("Solicitud requerida.");
            return errors;
        }

        if (request.EfectivoContado < 0)
        {
            errors.Add("Efectivo contado invalido.");
        }

        if (string.IsNullOrWhiteSpace(request.RowVersion))
        {
            errors.Add("Version de turno requerida.");
        }

        ValidateText(request.Observacion, "Observacion invalida.", errors);
        return errors;
    }

    private static void ValidateText(string? value, string error, List<string> errors)
    {
        if (value?.Length > MaxTextLength)
        {
            errors.Add(error);
        }
    }

    private static CajaTurnoResponse MapTurno(CajaTurnoQuery turno)
    {
        return new CajaTurnoResponse(
            turno.IdTurno,
            turno.CajaCodigo,
            turno.Estado,
            turno.UsuarioAperturaId,
            turno.UsuarioCierreId,
            ToUtc(turno.AperturaUtc),
            turno.CierreUtc is null ? null : ToUtc(turno.CierreUtc.Value),
            turno.FondoInicial,
            turno.EfectivoEsperado,
            turno.EfectivoContado,
            turno.Diferencia,
            turno.ObservacionApertura,
            turno.ObservacionCierre,
            turno.CierreCajaId,
            Convert.ToBase64String(turno.RowVersion));
    }

    private static MovimientoCajaResponse MapMovimiento(MovimientoCajaQuery movimiento)
    {
        return new MovimientoCajaResponse(
            movimiento.IdMovimiento,
            movimiento.IdTurno,
            movimiento.TipoMovimiento,
            movimiento.Origen,
            movimiento.Monto,
            movimiento.Moneda,
            ToUtc(movimiento.FechaHoraUtc),
            movimiento.UsuarioId,
            movimiento.Factura,
            movimiento.PagoId,
            movimiento.IngresoCajaId,
            movimiento.RetiroCajaId,
            movimiento.Referencia,
            movimiento.Observacion,
            movimiento.Estado,
            movimiento.ReversaDeMovimientoId);
    }

    private static DateTimeOffset ToUtc(DateTime value)
    {
        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
}
