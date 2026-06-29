namespace POS.Api.Contracts.Caja;

public sealed record ResumenMovimientoCajaResponse(
    string TipoMovimiento,
    int Cantidad,
    decimal Total);
