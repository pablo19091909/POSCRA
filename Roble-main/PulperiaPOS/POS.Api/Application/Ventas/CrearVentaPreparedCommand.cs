using POS.Api.Contracts.Ventas;

namespace POS.Api.Application.Ventas;

public sealed record CrearVentaPreparedCommand(
    CrearVentaRequest Request,
    int UsuarioId,
    byte[] RequestHash,
    string TraceId);
