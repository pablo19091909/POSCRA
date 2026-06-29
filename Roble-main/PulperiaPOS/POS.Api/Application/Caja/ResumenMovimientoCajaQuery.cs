namespace POS.Api.Application.Caja;

public sealed record ResumenMovimientoCajaQuery(
    string TipoMovimiento,
    int Cantidad,
    decimal Total);
