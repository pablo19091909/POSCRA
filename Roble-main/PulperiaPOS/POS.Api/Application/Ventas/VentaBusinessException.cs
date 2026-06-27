namespace POS.Api.Application.Ventas;

public sealed class VentaBusinessException : Exception
{
    public VentaBusinessException(VentaServiceStatus status, string safeMessage)
        : base(safeMessage)
    {
        Status = status;
        SafeMessage = safeMessage;
    }

    public VentaServiceStatus Status { get; }

    public string SafeMessage { get; }
}
