using POS.Api.Contracts.Ventas;

namespace POS.Api.Application.Ventas;

public sealed record ReversarVentaPreparedCommand(
    int Factura,
    ReversarVentaRequest Request,
    int UsuarioId,
    byte[] RequestHash,
    string TraceId);
