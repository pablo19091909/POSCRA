using POS.Api.Contracts.Ventas;

namespace POS.Api.Application.Ventas;

public sealed record ReversaVentaServiceResult(
    ReversaVentaServiceStatus Status,
    ReversarVentaResponse? Response,
    IReadOnlyCollection<string> Errors)
{
    public static ReversaVentaServiceResult Disabled() => new(ReversaVentaServiceStatus.Disabled, null, []);

    public static ReversaVentaServiceResult Invalid(IReadOnlyCollection<string> errors) =>
        new(ReversaVentaServiceStatus.Invalid, null, errors);

    public static ReversaVentaServiceResult Conflict(string error) =>
        new(ReversaVentaServiceStatus.Conflict, null, [error]);

    public static ReversaVentaServiceResult InProgress() =>
        new(ReversaVentaServiceStatus.InProgress, null, ["Solicitud de reversa en proceso."]);

    public static ReversaVentaServiceResult Success(ReversarVentaResponse response) =>
        new(ReversaVentaServiceStatus.Success, response, []);
}

public enum ReversaVentaServiceStatus
{
    Disabled,
    Invalid,
    Conflict,
    InProgress,
    Success
}
