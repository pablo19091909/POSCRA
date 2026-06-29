namespace POS.Api.Application.Caja;

public sealed class CajaBusinessException : Exception
{
    public CajaBusinessException(CajaServiceStatus status, string safeMessage)
        : base(safeMessage)
    {
        Status = status;
        SafeMessage = safeMessage;
    }

    public CajaServiceStatus Status { get; }

    public string SafeMessage { get; }
}
