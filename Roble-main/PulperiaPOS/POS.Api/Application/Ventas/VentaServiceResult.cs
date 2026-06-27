using POS.Api.Contracts.Ventas;

namespace POS.Api.Application.Ventas;

public sealed record VentaServiceResult(
    VentaServiceStatus Status,
    VentaResponse? Response,
    IReadOnlyCollection<string> Errors)
{
    public static VentaServiceResult Disabled() => new(VentaServiceStatus.Disabled, null, []);

    public static VentaServiceResult Invalid(IReadOnlyCollection<string> errors) =>
        new(VentaServiceStatus.Invalid, null, errors);

    public static VentaServiceResult Conflict(string error) =>
        new(VentaServiceStatus.Conflict, null, [error]);

    public static VentaServiceResult InProgress() =>
        new(VentaServiceStatus.InProgress, null, ["Solicitud de venta en proceso."]);

    public static VentaServiceResult Success(VentaResponse response) =>
        new(VentaServiceStatus.Success, response, []);
}

public enum VentaServiceStatus
{
    Disabled,
    Invalid,
    Conflict,
    InProgress,
    Success
}
