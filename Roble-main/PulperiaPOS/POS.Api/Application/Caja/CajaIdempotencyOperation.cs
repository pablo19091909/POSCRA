namespace POS.Api.Application.Caja;

public enum CajaIdempotencyOperation
{
    AbrirTurno,
    IngresoCaja,
    RetiroCaja,
    CerrarTurno,
    AjusteCaja,
    ReversaMovimiento
}
