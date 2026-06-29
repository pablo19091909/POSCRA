namespace POS.Api.Application.Caja;

public sealed record CajaServiceResult<T>(
    CajaServiceStatus Status,
    T? Response,
    IReadOnlyCollection<string> Errors)
{
    public static CajaServiceResult<T> Disabled() => new(CajaServiceStatus.Disabled, default, []);

    public static CajaServiceResult<T> Invalid(IReadOnlyCollection<string> errors) =>
        new(CajaServiceStatus.Invalid, default, errors);

    public static CajaServiceResult<T> NotFound(string error) =>
        new(CajaServiceStatus.NotFound, default, [error]);

    public static CajaServiceResult<T> Conflict(string error) =>
        new(CajaServiceStatus.Conflict, default, [error]);

    public static CajaServiceResult<T> Success(T response) =>
        new(CajaServiceStatus.Success, response, []);
}
