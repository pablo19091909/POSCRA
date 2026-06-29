namespace POS.Api.Application.Caja;

public enum CajaIdempotencyOperation
{
    IngresoCaja,
    RetiroCaja,
    CerrarTurno,
    AjusteCaja,
    ReversaMovimiento
}
