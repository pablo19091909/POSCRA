namespace POS.Api.Infrastructure.Logging;

public static class SafeErrorMessages
{
    public const string UnexpectedError = "Ocurrio un error inesperado.";
    public const string DatabaseUnavailable = "No fue posible validar la conexion de base de datos.";
    public const string AuthUnavailable = "Autenticacion no disponible.";
}
