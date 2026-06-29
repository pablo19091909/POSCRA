namespace POS.Api.Contracts.Caja;

public sealed record CerrarCajaTurnoRequest(
    decimal EfectivoContado,
    string? Observacion,
    string? RowVersion);
